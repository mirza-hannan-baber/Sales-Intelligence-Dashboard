using System.Globalization;
using System.Text;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public static class SqlResultFormatter
    {
        public static (bool IsConfident, string Answer) TryFormat(string question, SqlQueryResult result)
        {
            if (result.RowCount == 0)
            {
                return (true, "No matching data was found in your CRM for that question. Try adjusting the year, industry, or filters.");
            }

            if (result.RowCount == 1)
            {
                var answer = FormatSingleRow(question, result.Rows[0], result.Columns);
                if (answer != null) return (true, answer);
            }

            if (result.RowCount <= 10)
            {
                var answer = FormatSmallResultSet(question, result);
                if (answer != null) return (true, answer);
            }

            var summary = FormatSummary(question, result);
            return summary != null ? (true, summary) : (false, string.Empty);
        }

        private static string? FormatSingleRow(string question, Dictionary<string, object?> row, List<string> columns)
        {
            var lowerQ = question.ToLowerInvariant();

            var nameCol = FindColumn(columns, "owner", "salesagent", "agent", "employee", "name", "industry", "sector", "product", "region", "company");
            var valueCol = FindColumn(columns, "totalrevenue", "revenue", "total_sales", "sales", "wonrevenue", "value", "totalvalue", "sum", "amount");
            var countCol = FindColumn(columns, "dealcount", "count", "lostdeals", "woncount", "totaldeals", "deals");
            var rateCol = FindColumn(columns, "winrate", "rate", "percentage", "pct");
            var avgCol = FindColumn(columns, "average", "avg", "averagedays", "averagesalescycle", "averagevalue");

            var name = nameCol != null ? FormatValue(row[nameCol]) : null;
            var value = valueCol != null ? TryGetDecimal(row[valueCol]) : null;
            var count = countCol != null ? TryGetInt(row[countCol]) : null;
            var rate = rateCol != null ? TryGetDecimal(row[rateCol]) : null;
            var avg = avgCol != null ? TryGetDecimal(row[avgCol]) : null;

            if (name != null && rate.HasValue && (lowerQ.Contains("win rate") || rateCol?.Contains("win", StringComparison.OrdinalIgnoreCase) == true))
            {
                return $"{name} has a win rate of {rate.Value:F1}%.";
            }

            if (name != null && value.HasValue && (lowerQ.Contains("revenue") || lowerQ.Contains("sales") || valueCol?.Contains("revenue", StringComparison.OrdinalIgnoreCase) == true))
            {
                return $"{name} generated the highest won revenue at {FormatCurrency(value.Value)}.";
            }

            if (name != null && count.HasValue && (lowerQ.Contains("lost") || countCol?.Contains("lost", StringComparison.OrdinalIgnoreCase) == true))
            {
                return $"{name} has the most lost deals with {count.Value:N0} lost deals.";
            }

            if (name != null && count.HasValue)
            {
                return $"{name} leads with {count.Value:N0} deals.";
            }

            if (value.HasValue && !nameCol?.Contains("name", StringComparison.OrdinalIgnoreCase) == true && columns.Count <= 3)
            {
                var year = ExtractYearFromQuestion(question);
                var yearBit = year != null ? $" in {year}" : string.Empty;
                return $"Total won revenue{yearBit} is {FormatCurrency(value.Value)}.";
            }

            if (avg.HasValue)
            {
                var label = lowerQ.Contains("cycle") ? "average sales cycle" : "average value";
                return $"The {label} is {avg.Value:N1}.";
            }

            return FormatRowAsSentence(row, columns);
        }

        private static string? FormatSmallResultSet(string question, SqlQueryResult result)
        {
            var lowerQ = question.ToLowerInvariant();
            var labelCol = FindColumn(result.Columns, "owner", "salesagent", "agent", "employee", "name", "industry", "sector", "product", "region", "company", "month");
            var metricCol = FindColumn(result.Columns, "totalrevenue", "revenue", "winrate", "rate", "dealcount", "count", "lostdeals", "value", "totalvalue", "sum", "amount", "averagedays", "average");

            if (labelCol == null || metricCol == null)
            {
                return null;
            }

            var isWinRate = metricCol.Contains("win", StringComparison.OrdinalIgnoreCase) || metricCol.Contains("rate", StringComparison.OrdinalIgnoreCase);
            var isCurrency = metricCol.Contains("revenue", StringComparison.OrdinalIgnoreCase) || metricCol.Contains("value", StringComparison.OrdinalIgnoreCase) || metricCol.Contains("sales", StringComparison.OrdinalIgnoreCase);

            var parts = result.Rows.Take(5).Select(row =>
            {
                var label = FormatValue(row[labelCol]) ?? "Unknown";
                var metric = row[metricCol];
                if (isWinRate && TryDecimal(metric, out var rate))
                    return $"{label} ({rate:F1}%)";
                if (isCurrency && TryDecimal(metric, out var rev))
                    return $"{label} ({FormatCurrency(rev)})";
                if (TryInt(metric, out var cnt))
                    return $"{label} ({cnt:N0})";
                return $"{label} ({FormatValue(metric)})";
            }).ToList();

            if (lowerQ.Contains("compare") || lowerQ.Contains(" vs "))
            {
                return $"Comparison: {string.Join(", ", parts)}.";
            }

            if (lowerQ.Contains("top") || lowerQ.Contains("highest") || lowerQ.Contains("best") || lowerQ.Contains("most"))
            {
                var top = parts.FirstOrDefault() ?? "N/A";
                var metricLabel = isWinRate ? "win rate" : isCurrency ? "revenue" : "count";
                return $"Top result by {metricLabel}: {top}. Full ranking: {string.Join(", ", parts)}.";
            }

            return $"Results: {string.Join(", ", parts)}.";
        }

        private static string? FormatSummary(string question, SqlQueryResult result)
        {
            var labelCol = FindColumn(result.Columns, "owner", "salesagent", "agent", "industry", "sector", "product", "region", "company", "month");
            var metricCol = FindColumn(result.Columns, "totalrevenue", "revenue", "winrate", "dealcount", "count", "value", "sum");

            if (labelCol == null || metricCol == null) return null;

            var topRows = result.Rows.Take(5).Select(r => $"{FormatValue(r[labelCol])} ({FormatMetric(r[metricCol])})");
            return $"Top results for your question: {string.Join(", ", topRows)}.";
        }

        private static string? FormatRowAsSentence(Dictionary<string, object?> row, List<string> columns)
        {
            if (columns.Count == 0) return null;
            var parts = columns.Select(c => $"{Humanize(c)}: {FormatValue(row[c])}");
            return string.Join(", ", parts) + ".";
        }

        private static string? FindColumn(IEnumerable<string> columns, params string[] candidates)
        {
            var colList = columns.ToList();
            foreach (var candidate in candidates)
            {
                var match = colList.FirstOrDefault(c =>
                    c.Replace("_", "").Equals(candidate, StringComparison.OrdinalIgnoreCase) ||
                    c.Contains(candidate, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return colList.Count == 1 ? colList[0] : null;
        }

        private static string FormatMetric(object? value)
        {
            if (TryDecimal(value, out var d))
            {
                if (d is > 0 and < 1 or > 1 and <= 100)
                    return $"{d:F1}%";
                if (d >= 1000)
                    return FormatCurrency(d);
                return d.ToString("N0", CultureInfo.InvariantCulture);
            }
            return FormatValue(value) ?? "N/A";
        }

        private static string FormatCurrency(decimal value) => $"${value:N2}";

        private static string? FormatValue(object? value)
        {
            if (value == null) return null;
            if (value is decimal or double or float)
            {
                var d = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (Math.Abs(d) >= 1000) return FormatCurrency(d);
                return d.ToString("N2", CultureInfo.InvariantCulture);
            }
            return value.ToString();
        }

        private static decimal? TryGetDecimal(object? value) =>
            TryDecimal(value, out var d) ? d : null;

        private static int? TryGetInt(object? value) =>
            TryInt(value, out var i) ? i : null;

        private static bool TryDecimal(object? value, out decimal result)
        {
            result = 0;
            if (value == null) return false;
            if (value is decimal d) { result = d; return true; }
            if (value is double db) { result = (decimal)db; return true; }
            if (value is float f) { result = (decimal)f; return true; }
            if (value is int i) { result = i; return true; }
            if (value is long l) { result = l; return true; }
            return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryInt(object? value, out int result)
        {
            result = 0;
            if (value == null) return false;
            if (value is int i) { result = i; return true; }
            if (value is long l) { result = (int)l; return true; }
            return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private static string? ExtractYearFromQuestion(string question)
        {
            var match = System.Text.RegularExpressions.Regex.Match(question, @"\b(20[0-9]{2})\b");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string Humanize(string column) =>
            column.Replace("_", " ");
    }
}
