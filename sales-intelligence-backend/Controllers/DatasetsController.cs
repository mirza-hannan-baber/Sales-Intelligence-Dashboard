using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Models;
using SalesIntelligence.Api.Utilities;

namespace SalesIntelligence.Api.Controllers
{
    public class DatasetUploadRequest
    {
        public IFormFile File { get; set; } = default!;
        public string? UserEmail { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DatasetsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DatasetsController> _logger;

        public DatasetsController(ApplicationDbContext db, ILogger<DatasetsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetDatasets([FromQuery] string? userEmail = null, [FromQuery] string? role = null)
        {
            var query = _db.Datasets.AsNoTracking().Where(d => d.IsActive);

            // Superadmin sees ALL datasets.
            // Regular users see public datasets (UploadedByEmail null/empty or "public") AND datasets matching userEmail.
            if (!string.Equals(role, "Superadmin", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    var emailLower = userEmail.Trim().ToLower();
                    query = query.Where(d => string.IsNullOrEmpty(d.UploadedByEmail) ||
                                             d.UploadedByEmail == "public" ||
                                             d.UploadedByEmail.ToLower() == emailLower);
                }
                else
                {
                    query = query.Where(d => string.IsNullOrEmpty(d.UploadedByEmail) || d.UploadedByEmail == "public");
                }
            }

            var datasets = await query
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return Ok(datasets);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDatasetById(int id)
        {
            var dataset = await _db.Datasets.FindAsync(id);
            if (dataset == null) return NotFound();
            return Ok(dataset);
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDataset([FromForm] DatasetUploadRequest request)
        {
            var file = request?.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded or file is empty." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt" && extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new { message = "Only CSV, TXT, or Excel files are supported." });
            }

            try
            {
                List<string> lines = new();
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                        }
                    }
                }

                if (lines.Count <= 1)
                {
                    return BadRequest(new { message = "File must contain a header row and at least one data row." });
                }

                var headerIndex = CsvHelper.BuildHeaderIndex(lines[0]);

                var dataset = new Dataset
                {
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    FileName = file.FileName,
                    UploadedByEmail = request?.UserEmail?.Trim(),
                    UploadedAt = DateTime.UtcNow,
                    RowCount = lines.Count - 1,
                    DealsCount = 0,
                    IsActive = true
                };

                _db.Datasets.Add(dataset);
                await _db.SaveChangesAsync();

                var dealsList = new List<Deal>();
                var accountMap = new Dictionary<string, Account>();
                var agentMap = new Dictionary<string, Agent>();

                for (int i = 1; i < lines.Count; i++)
                {
                    var parts = CsvHelper.SplitCsvLine(lines[i]);
                    if (parts.Length == 0) continue;

                    var oppId = CsvHelper.GetField(parts, headerIndex, "opportunity_id");
                    if (string.IsNullOrWhiteSpace(oppId))
                    {
                        oppId = $"OPP_{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
                    }

                    var agentName = CsvHelper.GetField(parts, headerIndex, "sales_agent");
                    if (string.IsNullOrWhiteSpace(agentName))
                    {
                        agentName = CsvHelper.GetField(parts, headerIndex, "owner");
                    }
                    var product = CsvHelper.GetField(parts, headerIndex, "product");
                    var accName = CsvHelper.GetField(parts, headerIndex, "account");
                    if (string.IsNullOrWhiteSpace(accName))
                    {
                        accName = CsvHelper.GetField(parts, headerIndex, "company");
                    }
                    var stage = CsvHelper.GetField(parts, headerIndex, "deal_stage");
                    if (string.IsNullOrWhiteSpace(stage))
                    {
                        stage = CsvHelper.GetField(parts, headerIndex, "status");
                    }
                    var engageDateStr = CsvHelper.GetField(parts, headerIndex, "engage_date");
                    if (string.IsNullOrWhiteSpace(engageDateStr))
                    {
                        engageDateStr = CsvHelper.GetField(parts, headerIndex, "created_date");
                    }
                    var closeDateStr = CsvHelper.GetField(parts, headerIndex, "close_date");

                    decimal.TryParse(CsvHelper.GetField(parts, headerIndex, "deal_value_proposed"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal proposedValue);
                    if (proposedValue == 0)
                    {
                        decimal.TryParse(CsvHelper.GetField(parts, headerIndex, "proposed_value"), NumberStyles.Any, CultureInfo.InvariantCulture, out proposedValue);
                    }

                    decimal.TryParse(CsvHelper.GetField(parts, headerIndex, "close_value"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal closeValue);
                    if (closeValue == 0)
                    {
                        decimal.TryParse(CsvHelper.GetField(parts, headerIndex, "value"), NumberStyles.Any, CultureInfo.InvariantCulture, out closeValue);
                    }

                    var sector = CsvHelper.GetField(parts, headerIndex, "sector");
                    if (string.IsNullOrWhiteSpace(sector))
                    {
                        sector = CsvHelper.GetField(parts, headerIndex, "industry");
                    }

                    decimal.TryParse(CsvHelper.GetField(parts, headerIndex, "revenue"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal accRev);
                    if (accRev == 0)
                    {
                        decimal.TryParse(CsvHelper.GetField(parts, headerIndex, "account_revenue"), NumberStyles.Any, CultureInfo.InvariantCulture, out accRev);
                    }

                    int.TryParse(CsvHelper.GetField(parts, headerIndex, "employees"), NumberStyles.Any, CultureInfo.InvariantCulture, out int accEmployees);
                    var location = CsvHelper.GetField(parts, headerIndex, "office_location");
                    var manager = CsvHelper.GetField(parts, headerIndex, "manager");
                    var regionalOffice = CsvHelper.GetField(parts, headerIndex, "regional_office");

                    DateTime.TryParse(engageDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdDate);
                    DateTime.TryParse(closeDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime closeDate);

                    if (createdDate == default && closeDate == default)
                    {
                        createdDate = DateTime.UtcNow.AddMonths(-3);
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
                                DatasetId = dataset.Id,
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
                                DatasetId = dataset.Id,
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

                    int hashVal = Math.Abs((oppId + agentName + product + sector).GetHashCode());
                    double prob;
                    if (stage.Equals("Won", StringComparison.OrdinalIgnoreCase))
                    {
                        prob = 90.0 + (hashVal % 101) / 10.0;
                    }
                    else if (stage.Equals("Lost", StringComparison.OrdinalIgnoreCase))
                    {
                        prob = (hashVal % 150) / 10.0;
                    }
                    else
                    {
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
                        DatasetId = dataset.Id,
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

                foreach (var ag in agentMap.Values)
                {
                    ag.WinRate = ag.TotalDeals > 0 ? Math.Round((double)ag.WonDeals / ag.TotalDeals * 100, 1) : 0.0;
                    ag.PerformanceScore = ag.WinRate;
                }

                _db.Agents.AddRange(agentMap.Values);
                _db.Accounts.AddRange(accountMap.Values);
                await _db.SaveChangesAsync();

                const int batchSize = 500;
                for (int b = 0; b < dealsList.Count; b += batchSize)
                {
                    var batch = dealsList.Skip(b).Take(batchSize).ToList();
                    _db.Deals.AddRange(batch);
                    await _db.SaveChangesAsync();
                }

                dataset.DealsCount = dealsList.Count;
                _db.Datasets.Update(dataset);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Successfully uploaded dataset '{DatasetName}' (ID {DatasetId}) with {DealsCount} deals.", dataset.Name, dataset.Id, dataset.DealsCount);

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dataset upload for file {FileName}", file.FileName);
                return StatusCode(500, new { message = "Error processing uploaded file: " + ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDataset(int id)
        {
            var dataset = await _db.Datasets.FindAsync(id);
            if (dataset == null) return NotFound();

            var count = await _db.Datasets.CountAsync();
            if (count <= 1)
            {
                return BadRequest(new { message = "Cannot delete the default dataset when it is the only dataset remaining." });
            }

            var deals = _db.Deals.Where(d => d.DatasetId == id);
            _db.Deals.RemoveRange(deals);

            var accounts = _db.Accounts.Where(a => a.DatasetId == id);
            _db.Accounts.RemoveRange(accounts);

            var agents = _db.Agents.Where(a => a.DatasetId == id);
            _db.Agents.RemoveRange(agents);

            _db.Datasets.Remove(dataset);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Dataset '{dataset.Name}' deleted successfully." });
        }
    }
}
