using System.Text.Json;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IRecommendationAnalysisService
    {
        Task<Dictionary<string, object>> GatherInsightsAsync(CancellationToken cancellationToken = default);
        string FormatDeterministicRecommendations(Dictionary<string, object> insights);
    }

    public class RecommendationAnalysisService : IRecommendationAnalysisService
    {
        private readonly ISqlQueryExecutor _executor;
        private readonly ISchemaDiscoveryService _schema;
        private readonly ILogger<RecommendationAnalysisService> _logger;

        public RecommendationAnalysisService(
            ISqlQueryExecutor executor,
            ISchemaDiscoveryService schema,
            ILogger<RecommendationAnalysisService> logger)
        {
            _executor = executor;
            _schema = schema;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> GatherInsightsAsync(CancellationToken cancellationToken = default)
        {
            await _schema.GetSchemaAsync(cancellationToken);
            var insights = new Dictionary<string, object>();

            var queries = BuildAnalysisQueries();
            foreach (var (key, sql) in queries)
            {
                try
                {
                    var (valid, normalized, _) = SqlQueryValidator.ValidateAndNormalize(sql);
                    if (!valid) continue;
                    var result = await _executor.ExecuteAsync(normalized, cancellationToken);
                    insights[key] = CompactResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Recommendation analysis '{Key}' failed", key);
                }
            }

            return insights;
        }

        public string FormatDeterministicRecommendations(Dictionary<string, object> insights)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Based on your sales data, here are evidence-based recommendations:");
            sb.AppendLine();

            var recNum = 1;

            if (TryGetTop(insights, "top_industries_by_win_rate", out var topWinIndustry, out var topWinRate))
            {
                sb.AppendLine($"{recNum++}. Focus on **{topWinIndustry}** — it has the highest win rate at {topWinRate}.");
            }

            if (TryGetTop(insights, "top_industries_by_revenue", out var topRevIndustry, out var topRev))
            {
                sb.AppendLine($"{recNum++}. Prioritize **{topRevIndustry}** for revenue growth — it leads with {topRev} in won revenue.");
            }

            if (TryGetTop(insights, "most_lost_deals_by_industry", out var lostIndustry, out var lostCount))
            {
                sb.AppendLine($"{recNum++}. Investigate **{lostIndustry}** — it has the highest lost-deal volume ({lostCount} lost deals). Review pricing, competition, and sales approach.");
            }

            if (TryGetTop(insights, "top_products_by_revenue", out var topProduct, out var prodRev))
            {
                sb.AppendLine($"{recNum++}. Double down on **{topProduct}** — your top product by won revenue ({prodRev}).");
            }

            if (TryGetTop(insights, "top_employees_by_revenue", out var topEmployee, out var empRev))
            {
                sb.AppendLine($"{recNum++}. Replicate strategies from **{topEmployee}**, your top performer ({empRev} in won revenue).");
            }

            if (TryGetTop(insights, "longest_sales_cycle_industry", out var slowIndustry, out var cycleDays))
            {
                sb.AppendLine($"{recNum++}. Streamline sales cycles in **{slowIndustry}** — average cycle is {cycleDays} days, the longest in your data.");
            }

            if (TryGetTop(insights, "worst_region_by_win_rate", out var weakRegion, out var weakRate))
            {
                sb.AppendLine($"{recNum++}. Support the **{weakRegion}** region — it has the lowest win rate at {weakRate}.");
            }

            if (recNum == 1)
            {
                return "I analyzed your sales data but couldn't gather enough insights for recommendations. Please ensure your CRM data is loaded.";
            }

            return sb.ToString().Replace("**", "");
        }

        private Dictionary<string, string> BuildAnalysisQueries()
        {
            var sector = Col("industry", "Deals") ?? "Sector";
            var owner = Col("sales_agent", "Deals") ?? "Owner";
            var product = Col("product", "Deals") ?? "Product";
            var value = Col("deal_value", "Deals") ?? "Value";
            var status = Col("deal_status", "Deals") ?? "Status";
            var region = Col("region", "Deals") ?? "Region";
            var cycle = Col("sales_cycle", "Deals") ?? "DealDurationDays";
            var closeDate = Col("close_date", "Deals") ?? "CloseDate";
            var createdDate = Col("created_date", "Deals") ?? "CreatedDate";

            return new Dictionary<string, string>
            {
                ["top_industries_by_win_rate"] = $@"
SELECT {sector} AS industry,
       ROUND(CAST(SUM(CASE WHEN {status}='Won' THEN 1 ELSE 0 END) AS REAL)*100.0/COUNT(*),1) AS win_rate,
       COUNT(*) AS closed_deals
FROM Deals WHERE {status} IN ('Won','Lost')
GROUP BY {sector} HAVING COUNT(*)>=10
ORDER BY win_rate DESC LIMIT 5",

                ["top_industries_by_revenue"] = $@"
SELECT {sector} AS industry, SUM({value}) AS total_revenue, COUNT(*) AS won_deals
FROM Deals WHERE {status}='Won'
GROUP BY {sector} ORDER BY total_revenue DESC LIMIT 5",

                ["most_lost_deals_by_industry"] = $@"
SELECT {sector} AS industry, COUNT(*) AS lost_deals
FROM Deals WHERE {status}='Lost'
GROUP BY {sector} ORDER BY lost_deals DESC LIMIT 5",

                ["top_products_by_revenue"] = $@"
SELECT {product} AS product, SUM({value}) AS total_revenue, COUNT(*) AS won_deals
FROM Deals WHERE {status}='Won'
GROUP BY {product} ORDER BY total_revenue DESC LIMIT 5",

                ["top_employees_by_revenue"] = $@"
SELECT {owner} AS sales_agent, SUM({value}) AS total_revenue, COUNT(*) AS won_deals
FROM Deals WHERE {status}='Won' AND {owner} IS NOT NULL AND {owner}!=''
GROUP BY {owner} ORDER BY total_revenue DESC LIMIT 5",

                ["longest_sales_cycle_industry"] = $@"
SELECT {sector} AS industry, ROUND(AVG({cycle}),1) AS avg_cycle_days, COUNT(*) AS deals
FROM Deals WHERE {status} IN ('Won','Lost')
GROUP BY {sector} HAVING COUNT(*)>=10
ORDER BY avg_cycle_days DESC LIMIT 5",

                ["worst_region_by_win_rate"] = $@"
SELECT {region} AS region,
       ROUND(CAST(SUM(CASE WHEN {status}='Won' THEN 1 ELSE 0 END) AS REAL)*100.0/COUNT(*),1) AS win_rate,
       COUNT(*) AS closed_deals
FROM Deals WHERE {status} IN ('Won','Lost') AND {region} IS NOT NULL AND {region}!=''
GROUP BY {region} HAVING COUNT(*)>=10
ORDER BY win_rate ASC LIMIT 5",

                ["monthly_revenue_trend"] = $@"
SELECT strftime('%Y-%m', COALESCE({closeDate},{createdDate})) AS month,
       SUM({value}) AS revenue, COUNT(*) AS won_deals
FROM Deals WHERE {status}='Won'
GROUP BY month ORDER BY month DESC LIMIT 12"
            };
        }

        private string? Col(string semantic, string table) =>
            _schema.ResolveColumn(semantic, table)?.Name;

        private static object CompactResult(SqlQueryResult result) =>
            result.Rows.Take(5).Select(r =>
                r.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "")).ToList();

        private static bool TryGetTop(Dictionary<string, object> insights, string key, out string name, out string metric)
        {
            name = string.Empty;
            metric = string.Empty;
            if (!insights.TryGetValue(key, out var raw)) return false;

            var rows = raw as List<Dictionary<string, string>>;
            if (rows == null || rows.Count == 0) return false;

            var row = rows[0];
            var labelKey = row.Keys.FirstOrDefault(k =>
                k.Contains("industry", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("sector", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("agent", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("employee", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("product", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("region", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("company", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("month", StringComparison.OrdinalIgnoreCase));

            name = labelKey != null ? row[labelKey] : row.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "N/A";

            var metricKey = row.Keys.FirstOrDefault(k =>
                k.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("revenue", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("lost", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("cycle", StringComparison.OrdinalIgnoreCase) ||
                k.Contains("count", StringComparison.OrdinalIgnoreCase));

            if (metricKey != null)
            {
                metric = row[metricKey];
                if (metricKey.Contains("revenue", StringComparison.OrdinalIgnoreCase) &&
                    decimal.TryParse(metric, out var rev))
                {
                    metric = $"${rev:N0}";
                }
                else if (metricKey.Contains("rate", StringComparison.OrdinalIgnoreCase))
                {
                    metric = metric.EndsWith('%') ? metric : $"{metric}%";
                }
                else if (metricKey.Contains("cycle", StringComparison.OrdinalIgnoreCase))
                {
                    metric = $"{metric} days";
                }
            }

            return !string.IsNullOrWhiteSpace(name);
        }
    }
}
