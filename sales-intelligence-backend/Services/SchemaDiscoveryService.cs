using System.Text;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Models;
using SalesIntelligence.Api.Prompts;
using SalesIntelligence.Api.Utilities;

namespace SalesIntelligence.Api.Services
{
    public class SchemaDiscoveryService : ISchemaDiscoveryService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SchemaDiscoveryService> _logger;
        private DatasetSchema? _cache;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly (string Tag, string[] Patterns, string Meaning)[] SemanticPatterns =
        {
            ("sales_agent", new[] { "sales_agent", "salesagent", "agent", "rep", "owner", "salesperson", "sales_rep", "employee" }, "Sales employee / agent who owns the deal"),
            ("company", new[] { "account", "company", "customer", "client" }, "Customer / account / company name"),
            ("product", new[] { "product", "sku", "item" }, "Product sold on the deal"),
            ("industry", new[] { "sector", "industry", "market", "vertical" }, "Industry or market sector"),
            ("deal_won_value", new[] { "close_value", "closevalue", "value" }, "Actual won closed value of deal (Note: Value is 0 for Lost deals!)"),
            ("deal_proposed_value", new[] { "proposed_value", "proposedvalue", "deal_amount", "amount", "deal_value" }, "Original proposed value of deal (Use ProposedValue for lost deal values!)"),
            ("agent_revenue", new[] { "totalrevenue", "agent_revenue", "agent_sales" }, "Total won revenue of sales agent (in Agents table)"),
            ("account_revenue", new[] { "account_revenue", "company_revenue" }, "Account-level annual revenue"),
            ("deal_status", new[] { "deal_stage", "stage", "status", "deal_status" }, "Deal status ('Won', 'Lost', 'In Progress')"),
            ("created_date", new[] { "engage_date", "createddate", "created_date", "start_date" }, "Deal engagement / created date"),
            ("close_date", new[] { "close_date", "closedate", "closing_date" }, "Deal close date"),
            ("sales_cycle", new[] { "sales_cycle_days", "salescycle", "deal_duration", "dealdurationdays", "cycle_days" }, "Sales cycle / deal duration in days"),
            ("region", new[] { "region", "regional_office", "office", "territory" }, "Geographic region or office"),
            ("opportunity_id", new[] { "opportunity_id", "opportunityid", "opp_id", "deal_id" }, "Unique deal / opportunity identifier"),
            ("probability", new[] { "probability", "prob", "win_probability" }, "Deal win probability"),
            ("location", new[] { "office_location", "location", "city" }, "Office or account location"),
            ("manager", new[] { "manager", "sales_manager" }, "Sales manager"),
            ("employees", new[] { "employees", "employee_count", "headcount" }, "Number of employees at account"),
            ("is_won", new[] { "is_won", "won", "iswon" }, "Whether the deal was won (1/0 or true/false)"),
            ("is_closed", new[] { "is_closed", "closed", "isclosed" }, "Whether the deal is closed")
        };

        public SchemaDiscoveryService(IServiceScopeFactory scopeFactory, ILogger<SchemaDiscoveryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task WarmCacheAsync(CancellationToken cancellationToken = default)
        {
            await GetSchemaAsync(cancellationToken);
        }

        public string GetPromptText() => _cache?.PromptText ?? string.Empty;

        public ColumnMetadata? ResolveColumn(string semanticTag, string? preferredTable = null)
        {
            if (_cache == null || !_cache.SemanticIndex.TryGetValue(semanticTag, out var cols))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(preferredTable))
            {
                return cols.FirstOrDefault(c =>
                    string.Equals(c.Table, preferredTable, StringComparison.OrdinalIgnoreCase)) ?? cols.FirstOrDefault();
            }

            return cols.FirstOrDefault(c => c.Source == "database") ?? cols.FirstOrDefault();
        }

        public async Task<DatasetSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
        {
            if (_cache != null) return _cache;

            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (_cache != null) return _cache;
                _cache = await DiscoverSchemaAsync(cancellationToken);
                _logger.LogInformation(
                    "Schema discovered: CSV={CsvRows} rows, DB tables={TableCount}",
                    _cache.Csv?.RowCount ?? 0,
                    _cache.Tables.Count);
                return _cache;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<DatasetSchema> DiscoverSchemaAsync(CancellationToken cancellationToken)
        {
            var schema = new DatasetSchema();
            schema.Csv = await ReadCsvMetadataAsync(cancellationToken);
            schema.Tables = await ReadDatabaseMetadataAsync(cancellationToken);
            BuildSemanticIndex(schema);
            schema.PromptText = BuildPromptText(schema);
            return schema;
        }

        private static async Task<CsvDatasetMetadata?> ReadCsvMetadataAsync(CancellationToken cancellationToken)
        {
            var path = CsvHelper.FindCsvPath();
            if (path == null) return null;

            var meta = new CsvDatasetMetadata { FilePath = path };
            var columnSamples = new Dictionary<string, List<string>>();

            using var reader = new StreamReader(path);
            var headerLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(headerLine)) return meta;

            var headers = CsvHelper.SplitCsvLine(headerLine);
            meta.ColumnCount = headers.Length;
            foreach (var h in headers)
            {
                columnSamples[h] = new List<string>();
            }

            var rowCount = 0;
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                rowCount++;
                if (rowCount > 500) continue;

                var parts = CsvHelper.SplitCsvLine(line);
                for (var i = 0; i < Math.Min(headers.Length, parts.Length); i++)
                {
                    if (columnSamples[headers[i]].Count < 5 && !string.IsNullOrWhiteSpace(parts[i]))
                    {
                        columnSamples[headers[i]].Add(parts[i]);
                    }
                }
            }

            meta.RowCount = rowCount;
            meta.Columns = headers.Select(h =>
            {
                var tags = InferSemanticTags(h);
                return new ColumnMetadata
                {
                    Name = h,
                    Source = "csv",
                    DataType = CsvHelper.InferType(columnSamples[h]),
                    SampleValues = columnSamples[h].Take(3).ToList(),
                    SemanticTags = tags,
                    BusinessMeaning = DescribeSemantics(tags),
                    CsvColumnName = h
                };
            }).ToList();

            return meta;
        }

        private async Task<List<TableMetadata>> ReadDatabaseMetadataAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tables = new List<TableMetadata>();

            var entityTypes = db.Model.GetEntityTypes()
                .Where(e => e.ClrType.Namespace == "SalesIntelligence.Api.Models" &&
                            e.ClrType.Name is "Deal" or "Agent" or "Account")
                .OrderBy(e => e.ClrType.Name);

            foreach (var entity in entityTypes)
            {
                var tableName = entity.GetTableName() ?? entity.ClrType.Name;
                var tableMeta = new TableMetadata { Name = tableName };

                tableMeta.RowCount = tableName switch
                {
                    "Deals" => await db.Deals.CountAsync(cancellationToken),
                    "Agents" => await db.Agents.CountAsync(cancellationToken),
                    "Accounts" => await db.Accounts.CountAsync(cancellationToken),
                    _ => 0
                };

                foreach (var prop in entity.GetProperties())
                {
                    var colName = prop.GetColumnName();
                    var tags = InferSemanticTags(colName);
                    var samples = await GetSampleValuesAsync(db, tableName, colName, cancellationToken);

                    tableMeta.Columns.Add(new ColumnMetadata
                    {
                        Name = colName,
                        Source = "database",
                        Table = tableName,
                        DataType = MapClrType(prop.ClrType),
                        SampleValues = samples,
                        SemanticTags = tags,
                        BusinessMeaning = DescribeSemantics(tags)
                    });
                }

                tables.Add(tableMeta);
            }

            return tables;
        }

        private static async Task<List<string>> GetSampleValuesAsync(
            ApplicationDbContext db,
            string table,
            string column,
            CancellationToken cancellationToken)
        {
            try
            {
                var sql = $"SELECT DISTINCT \"{column}\" FROM \"{table}\" WHERE \"{column}\" IS NOT NULL LIMIT 3";
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync(cancellationToken);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var samples = new List<string>();
                while (await reader.ReadAsync(cancellationToken) && samples.Count < 3)
                {
                    samples.Add(reader.GetValue(0)?.ToString() ?? string.Empty);
                }
                return samples;
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void BuildSemanticIndex(DatasetSchema schema)
        {
            var allColumns = schema.Tables.SelectMany(t => t.Columns)
                .Concat(schema.Csv?.Columns ?? Enumerable.Empty<ColumnMetadata>());

            foreach (var col in allColumns)
            {
                foreach (var tag in col.SemanticTags)
                {
                    if (!schema.SemanticIndex.ContainsKey(tag))
                        schema.SemanticIndex[tag] = new List<ColumnMetadata>();
                    schema.SemanticIndex[tag].Add(col);
                }
            }
        }

        private static string BuildPromptText(DatasetSchema schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DATASET (auto-discovered — use these exact names in SQL):");
            sb.AppendLine();

            if (schema.Csv != null)
            {
                sb.AppendLine($"CSV source: {Path.GetFileName(schema.Csv.FilePath)}");
                sb.AppendLine($"CSV rows: {schema.Csv.RowCount:N0} | CSV columns: {schema.Csv.ColumnCount}");
                sb.AppendLine("CSV columns (imported into database at startup):");
                foreach (var col in schema.Csv.Columns)
                {
                    var samples = col.SampleValues.Count > 0 ? string.Join(", ", col.SampleValues) : "n/a";
                    sb.AppendLine($"  - {col.Name} ({col.DataType}) → {col.BusinessMeaning} | samples: {samples}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("DATABASE TABLES (runtime source of truth for queries):");
            foreach (var table in schema.Tables)
            {
                sb.AppendLine($"TABLE {table.Name} ({table.RowCount:N0} rows):");
                foreach (var col in table.Columns)
                {
                    var csvLink = FindLinkedCsvColumn(schema, col);
                    var csvNote = csvLink != null ? $" | csv_source: {csvLink}" : string.Empty;
                    var samples = col.SampleValues.Count > 0 ? string.Join(", ", col.SampleValues) : "n/a";
                    sb.AppendLine($"  - {col.Name} ({col.DataType}) → {col.BusinessMeaning}{csvNote} | samples: {samples}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("SEMANTIC ALIASES (users may use any of these words — map to the database column above):");
            foreach (var (tag, _, meaning) in SemanticPatterns)
            {
                if (!schema.SemanticIndex.TryGetValue(tag, out var cols)) continue;
                var dbCols = cols.Where(c => c.Source == "database")
                    .Select(c => $"{c.Table}.{c.Name}")
                    .Distinct();
                if (!dbCols.Any()) continue;
                sb.AppendLine($"  - {tag}: {meaning} → use {string.Join(" or ", dbCols)}");
            }

            sb.AppendLine();
            sb.AppendLine(SalesAssistantPrompt.BusinessRulesPrompt);
            return sb.ToString();
        }

        private static string? FindLinkedCsvColumn(DatasetSchema schema, ColumnMetadata dbCol)
        {
            if (schema.Csv == null) return null;
            var sharedTags = dbCol.SemanticTags;
            var match = schema.Csv.Columns.FirstOrDefault(c => c.SemanticTags.Any(t => sharedTags.Contains(t)));
            return match?.Name;
        }

        private static List<string> InferSemanticTags(string columnName)
        {
            var normalized = columnName.ToLowerInvariant().Replace("_", "").Replace(" ", "");
            var tags = new List<string>();

            foreach (var (tag, patterns, _) in SemanticPatterns)
            {
                if (patterns.Any(p => normalized.Contains(p.Replace("_", ""), StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(tag);
                }
            }

            if (tags.Count == 0)
            {
                tags.Add("other");
            }

            return tags.Distinct().ToList();
        }

        private static string DescribeSemantics(IReadOnlyList<string> tags)
        {
            if (tags.Count == 0 || tags[0] == "other") return "General data field";
            var match = SemanticPatterns.FirstOrDefault(p => tags.Contains(p.Tag));
            return match.Meaning != null ? match.Meaning : tags[0];
        }

        private static string MapClrType(Type clrType)
        {
            var t = Nullable.GetUnderlyingType(clrType) ?? clrType;
            return t.Name switch
            {
                "Int32" or "Int64" => "integer",
                "Decimal" or "Double" or "Single" => "decimal",
                "DateTime" => "datetime",
                "Boolean" => "boolean",
                _ => "text"
            };
        }
    }
}
