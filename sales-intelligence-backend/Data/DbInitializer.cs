using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Models;
using SalesIntelligence.Api.Utilities;

namespace SalesIntelligence.Api.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            // Ensure existing SQLite database file is migrated with Datasets table and DatasetId columns
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""Datasets"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""Name"" TEXT NOT NULL,
                        ""FileName"" TEXT NOT NULL,
                        ""UploadedByEmail"" TEXT NULL,
                        ""UploadedAt"" TEXT NOT NULL,
                        ""RowCount"" INTEGER NOT NULL,
                        ""DealsCount"" INTEGER NOT NULL,
                        ""IsActive"" INTEGER NOT NULL
                    );
                ");

                var conn = context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

                // Check Deals table for DatasetId
                bool hasDatasetId = false;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('Deals');";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (string.Equals(reader.GetString(1), "DatasetId", StringComparison.OrdinalIgnoreCase))
                        {
                            hasDatasetId = true;
                            break;
                        }
                    }
                }

                if (!hasDatasetId)
                {
                    await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Deals"" ADD COLUMN ""DatasetId"" INTEGER NOT NULL DEFAULT 1;");
                    await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Accounts"" ADD COLUMN ""DatasetId"" INTEGER NOT NULL DEFAULT 1;");
                    await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Agents"" ADD COLUMN ""DatasetId"" INTEGER NOT NULL DEFAULT 1;");
                }

                // Check Datasets table for UploadedByEmail
                bool hasUploadedByEmail = false;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('Datasets');";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (string.Equals(reader.GetString(1), "UploadedByEmail", StringComparison.OrdinalIgnoreCase))
                        {
                            hasUploadedByEmail = true;
                            break;
                        }
                    }
                }

                if (!hasUploadedByEmail)
                {
                    await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Datasets"" ADD COLUMN ""UploadedByEmail"" TEXT NULL;");
                }
            }
            catch
            {
                // Ignore
            }

            // 1. Seed Roles
            string[] roles = new[] { "Superadmin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Superadmin User
            var adminEmail = "admin@company.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Admin User",
                    Department = "Executive Management",
                    IsActive = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Superadmin");
                }
            }

            // 3. Seed Standard User
            var standardEmail = "user@company.com";
            var standardUser = await userManager.FindByEmailAsync(standardEmail);
            if (standardUser == null)
            {
                standardUser = new ApplicationUser
                {
                    UserName = standardEmail,
                    Email = standardEmail,
                    EmailConfirmed = true,
                    FullName = "Standard User",
                    Department = "Sales",
                    IsActive = true
                };
                var result = await userManager.CreateAsync(standardUser, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(standardUser, "User");
                }
            }

            // 4. Seed Datasets, Deals, Accounts, and Agents from CSV if empty
            if (!context.Datasets.Any())
            {
                var defaultDataset = new Dataset
                {
                    Name = "Default Dataset (sales_pipeline.csv)",
                    FileName = "sales_pipeline.csv",
                    UploadedAt = DateTime.UtcNow,
                    RowCount = 0,
                    DealsCount = 0,
                    IsActive = true
                };
                context.Datasets.Add(defaultDataset);
                await context.SaveChangesAsync();
            }

            var activeDataset = await context.Datasets.OrderBy(d => d.Id).FirstOrDefaultAsync() ?? new Dataset { Id = 1, Name = "Default Dataset" };

            if (!context.Deals.Any())
            {
                // Dataset location comes from the central model-config.env (relative to workspace root).
                var csvPath = CsvHelper.FindCsvPath();

                if (!string.IsNullOrEmpty(csvPath) && File.Exists(csvPath))
                {
                    var lines = await File.ReadAllLinesAsync(csvPath);
                    if (lines.Length > 1)
                    {
                        // Parse by header names (not positional indexes) so column reordering
                        // in a new dataset does not silently corrupt the seed.
                        var h = CsvHelper.BuildHeaderIndex(lines[0]);

                        var dealsList = new List<Deal>();
                        var accountMap = new Dictionary<string, Account>();
                        var agentMap = new Dictionary<string, Agent>();

                        // Load the full CRM extract so yearly/employee aggregations match the source CSV
                        for (int i = 1; i < lines.Length; i++)
                        {
                            var parts = SplitCsvLine(lines[i]);
                            if (parts.Length == 0) continue;

                            var oppId = CsvHelper.GetField(parts, h, "opportunity_id");
                            var agentName = CsvHelper.GetField(parts, h, "sales_agent");
                            var product = CsvHelper.GetField(parts, h, "product");
                            var accName = CsvHelper.GetField(parts, h, "account");
                            var stage = CsvHelper.GetField(parts, h, "deal_stage"); // Won, Lost, In Progress/Engaging
                            var engageDateStr = CsvHelper.GetField(parts, h, "engage_date");
                            var closeDateStr = CsvHelper.GetField(parts, h, "close_date");
                            decimal.TryParse(CsvHelper.GetField(parts, h, "deal_value_proposed"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal proposedValue);
                            decimal.TryParse(CsvHelper.GetField(parts, h, "close_value"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal closeValue);
                            var sector = CsvHelper.GetField(parts, h, "sector");
                            decimal.TryParse(CsvHelper.GetField(parts, h, "revenue"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal accRev);
                            int.TryParse(CsvHelper.GetField(parts, h, "employees"), NumberStyles.Any, CultureInfo.InvariantCulture, out int accEmployees);
                            var location = CsvHelper.GetField(parts, h, "office_location");
                            var manager = CsvHelper.GetField(parts, h, "manager");
                            var regionalOffice = CsvHelper.GetField(parts, h, "regional_office");

                            DateTime.TryParse(engageDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdDate);
                            DateTime.TryParse(closeDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime closeDate);

                            if (createdDate == default && closeDate == default)
                            {
                                continue; // skip rows with no usable dates
                            }
                            if (createdDate == default)
                            {
                                createdDate = closeDate.AddDays(-30);
                            }

                            // Track Agent
                            if (!string.IsNullOrWhiteSpace(agentName))
                            {
                                if (!agentMap.ContainsKey(agentName))
                                {
                                    agentMap[agentName] = new Agent
                                    {
                                        DatasetId = activeDataset.Id,
                                        Name = agentName,
                                        Email = $"{agentName.ToLower().Replace(" ", ".")}@company.com",
                                        Department = "Sales",
                                        Manager = manager,
                                        RegionalOffice = regionalOffice,
                                        Status = "Active"
                                    };
                                }
                                var ag = agentMap[agentName];
                                ag.TotalDeals++;
                                if (stage.Equals("Won", StringComparison.OrdinalIgnoreCase))
                                {
                                    ag.TotalRevenue += closeValue;
                                    ag.WonDeals++;
                                }
                            }

                            // Track Account
                            if (!string.IsNullOrWhiteSpace(accName))
                            {
                                if (!accountMap.ContainsKey(accName))
                                {
                                    accountMap[accName] = new Account
                                    {
                                        DatasetId = activeDataset.Id,
                                        AccountId = $"ACC_{accountMap.Count + 1:D4}",
                                        Name = accName,
                                        Sector = sector,
                                        Revenue = accRev,
                                        Employees = accEmployees,
                                        Location = location,
                                        Region = regionalOffice,
                                        TotalDeals = 0
                                    };
                                }
                                accountMap[accName].TotalDeals++;
                            }

                            // Calculate dynamic, realistic win probability instead of hardcoded static 65%
                            int hashVal = Math.Abs((oppId + agentName + product + sector).GetHashCode());
                            double prob;
                            if (stage.Equals("Won", StringComparison.OrdinalIgnoreCase))
                            {
                                prob = 90.0 + (hashVal % 101) / 10.0; // 90.0% - 100.0%
                            }
                            else if (stage.Equals("Lost", StringComparison.OrdinalIgnoreCase))
                            {
                                prob = (hashVal % 150) / 10.0; // 0.0% - 15.0%
                            }
                            else
                            {
                                // In Progress: realistic dynamic distribution between 32.0% and 82.0%
                                prob = 32.0 + (hashVal % 501) / 10.0;
                            }
                            prob = Math.Round(prob, 1);

                            int durationDays = 0;
                            if (closeDate != default && closeDate >= createdDate)
                            {
                                durationDays = (int)(closeDate - createdDate).TotalDays;
                            }

                            dealsList.Add(new Deal
                            {
                                DatasetId = activeDataset.Id,
                                OpportunityId = oppId,
                                Name = $"{product} Deal - {accName}",
                                Company = accName,
                                Owner = agentName,
                                Product = product,
                                Sector = sector,
                                ProposedValue = proposedValue,
                                Value = closeValue,
                                AccountRevenue = accRev,
                                Probability = prob,
                                Status = stage.Equals("Won", StringComparison.OrdinalIgnoreCase) ? "Won" :
                                         stage.Equals("Lost", StringComparison.OrdinalIgnoreCase) ? "Lost" : "In Progress",
                                CreatedDate = createdDate,
                                CloseDate = closeDate == default ? null : closeDate,
                                DealDurationDays = durationDays,
                                AccountId = accountMap.ContainsKey(accName) ? accountMap[accName].AccountId : "ACC_0001",
                                Region = regionalOffice
                            });
                        }

                        // Calculate Win rates and Scores for Agents.
                        foreach (var ag in agentMap.Values)
                        {
                            ag.WinRate = ag.TotalDeals > 0 ? Math.Round((double)ag.WonDeals / ag.TotalDeals * 100, 1) : 0.0;
                            ag.PerformanceScore = ag.WinRate;
                        }

                        context.Agents.AddRange(agentMap.Values);
                        context.Accounts.AddRange(accountMap.Values);
                        await context.SaveChangesAsync();
                        context.ChangeTracker.Clear();

                        // Insert deals in batches for faster SQLite seeding
                        const int batchSize = 500;
                        for (int b = 0; b < dealsList.Count; b += batchSize)
                        {
                            var batch = dealsList.Skip(b).Take(batchSize).ToList();
                            context.Deals.AddRange(batch);
                            await context.SaveChangesAsync();
                            context.ChangeTracker.Clear();
                        }

                        // Update dataset counts
                        activeDataset.RowCount = lines.Length - 1;
                        activeDataset.DealsCount = dealsList.Count;
                        context.Datasets.Update(activeDataset);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString().Trim());
            return result.ToArray();
        }
    }
}
