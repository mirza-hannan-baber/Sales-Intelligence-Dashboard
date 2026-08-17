using System.Text;
using System.Text.Json;

namespace SalesIntelligence.Api.Services
{
    public static class DataAnswerFormatter
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string Format(string question, object calcResult)
        {
            try
            {
                var json = JsonSerializer.Serialize(calcResult, JsonOpts);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    return FormatArray(question, root);
                }

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return $"The calculated answer is {root}.";
                }

                if (root.TryGetProperty("message", out var msgEl))
                {
                    return msgEl.GetString() ?? json;
                }

                if (root.TryGetProperty("uniqueSalesAgents", out var agentsEl) &&
                    !root.TryGetProperty("totalRevenue", out _) &&
                    !root.TryGetProperty("topEmployee", out _) &&
                    !root.TryGetProperty("employees", out _))
                {
                    var yearBit = YearSuffix(root);
                    var dealsBit = root.TryGetProperty("totalDeals", out var td)
                        ? $" (based on {td.GetInt32():N0} deal records in scope)"
                        : string.Empty;
                    return $"There are {agentsEl.GetInt32()} unique sales agents{yearBit} in your data{dealsBit}.";
                }

                if (root.TryGetProperty("topEmployee", out var topEl))
                {
                    var name = topEl.GetString() ?? "N/A";
                    decimal rev = GetDecimal(root, "revenue");
                    var wonDeals = root.TryGetProperty("wonDeals", out var wd) ? wd.GetInt32() : 0;
                    var yearBit = YearSuffix(root);
                    return $"{name} performed best{yearBit} with ${rev:N2} in won revenue ({wonDeals:N0} won deals).";
                }

                if (root.TryGetProperty("bottomEmployee", out var bottomEl))
                {
                    var name = bottomEl.GetString() ?? "N/A";
                    decimal rev = GetDecimal(root, "revenue");
                    var yearBit = YearSuffix(root);
                    return $"{name} generated the lowest employee revenue{yearBit} at ${rev:N2}.";
                }

                if (root.TryGetProperty("topIndustry", out var topIndCountEl) &&
                    root.TryGetProperty("dealCount", out _) &&
                    !root.TryGetProperty("winRate", out _))
                {
                    var industry = topIndCountEl.GetString() ?? "N/A";
                    var count = root.TryGetProperty("dealCount", out var dc) ? dc.GetInt32() : 0;
                    var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
                    var yearBit = YearSuffix(root);
                    if (string.Equals(status, "Lost", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{industry} has the most lost deals{yearBit} with {count:N0} lost deals.";
                    }
                    if (string.Equals(status, "Won", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{industry} has the most won deals{yearBit} with {count:N0} won deals.";
                    }
                    return $"{industry} has the most deals{yearBit} with {count:N0} deals.";
                }

                if (root.TryGetProperty("employees", out var empEl) && empEl.ValueKind == JsonValueKind.Array)
                {
                    return FormatEmployeeList(root, empEl);
                }

                if (root.TryGetProperty("industries", out var indEl) && indEl.ValueKind == JsonValueKind.Array)
                {
                    return FormatIndustryList(question, root, indEl);
                }

                if (root.TryGetProperty("months", out var monthEl) && monthEl.ValueKind == JsonValueKind.Array)
                {
                    return FormatMonthList(root, monthEl);
                }

                if (root.TryGetProperty("topIndustry", out var topIndEl))
                {
                    var rate = GetDecimal(root, "winRate");
                    var yearBit = YearSuffix(root);
                    if (root.TryGetProperty("industries", out var indArr) && indArr.ValueKind == JsonValueKind.Array)
                    {
                        var topRow = indArr.EnumerateArray().FirstOrDefault();
                        if (topRow.ValueKind == JsonValueKind.Object && topRow.TryGetProperty("wonDeals", out _) && topRow.TryGetProperty("totalDeals", out _))
                        {
                            return $"{topIndEl.GetString()} has the highest win rate{yearBit} at {rate:F1}% ({topRow.GetProperty("wonDeals").GetInt32()} won / {topRow.GetProperty("totalDeals").GetInt32()} closed).";
                        }
                    }
                    return $"{topIndEl.GetString()} has the highest win rate{yearBit} at {rate:F1}%.";
                }

                if (root.TryGetProperty("winRate", out var wrEl) && !root.TryGetProperty("totalRevenue", out _))
                {
                    var total = root.TryGetProperty("totalDeals", out var td) ? td.GetInt32() : 0;
                    var won = root.TryGetProperty("wonDeals", out var wd) ? wd.GetInt32() : 0;
                    return $"Overall win rate{YearSuffix(root)} is {wrEl.GetDecimal():F1}% ({won:N0} won out of {total:N0} closed deals).";
                }

                if (root.TryGetProperty("topDeal", out var topDealEl))
                {
                    var owner = root.TryGetProperty("owner", out var o) ? o.GetString() : "N/A";
                    return $"Largest won deal{YearSuffix(root)} is \"{topDealEl.GetString()}\" owned by {owner} at ${GetDecimal(root, "revenue"):N2}.";
                }

                if (root.TryGetProperty("totalRevenue", out _))
                {
                    var total = GetDecimal(root, "totalRevenue");
                    var yearBit = YearSuffix(root);
                    var monthBit = root.TryGetProperty("month", out var mEl) && mEl.ValueKind != JsonValueKind.Null
                        ? $" in month {mEl.GetInt32()}"
                        : string.Empty;
                    var agentBit = root.TryGetProperty("uniqueSalesAgents", out var ua)
                        ? $" across {ua.GetInt32()} sales agents"
                        : string.Empty;
                    var wonBit = root.TryGetProperty("wonDealsCount", out var wc)
                        ? $" from {wc.GetInt32():N0} won deals"
                        : string.Empty;
                    return $"Total revenue{yearBit}{monthBit} is ${total:N2}{agentBit}{wonBit}.";
                }

                if (root.TryGetProperty("totalDeals", out var dealsEl) && !root.TryGetProperty("totalRevenue", out _))
                {
                    var count = dealsEl.GetInt32();
                    var won = root.TryGetProperty("wonDeals", out var w) ? w.GetInt32() : (int?)null;
                    var lost = root.TryGetProperty("lostDeals", out var l) ? l.GetInt32() : (int?)null;
                    var yearBit = YearSuffix(root);
                    var sb = new StringBuilder($"There are {count:N0} deals{yearBit} in scope.");
                    if (won.HasValue) sb.Append($" Won: {won.Value:N0}.");
                    if (lost.HasValue) sb.Append($" Lost: {lost.Value:N0}.");
                    if (root.TryGetProperty("uniqueSalesAgents", out var ua2))
                    {
                        sb.Append($" Distinct sales agents: {ua2.GetInt32()}.");
                    }
                    return sb.ToString();
                }

                if (root.TryGetProperty("averageValue", out _))
                {
                    var avg = GetDecimal(root, "averageValue");
                    var label = root.TryGetProperty("metric", out var metricEl)
                        ? metricEl.GetString()
                        : "value";
                    var yearBit = YearSuffix(root);
                    return $"Average {label}{yearBit} is {avg:N2}.";
                }

                if (root.TryGetProperty("nextQuarterTotal", out var nq) && nq.TryGetDecimal(out var nqVal))
                {
                    return $"The next-quarter revenue forecast is ${nqVal:N0}.";
                }
            }
            catch
            {
                // fall through
            }

            return "I found matching data but couldn't format a summary. Please try a more specific question.";
        }

        private static string FormatEmployeeList(JsonElement root, JsonElement empEl)
        {
            var yearBit = YearSuffix(root);
            var total = GetDecimal(root, "totalRevenue");
            var topNames = empEl.EnumerateArray()
                .Take(5)
                .Select(e => $"{e.GetProperty("salesAgent").GetString()} (${GetDecimal(e, "revenue"):N0})")
                .ToList();
            var count = root.TryGetProperty("uniqueSalesAgents", out var ua)
                ? ua.GetInt32()
                : empEl.GetArrayLength();
            return $"Employee revenue{yearBit} totals ${total:N2} across {count} sales agents. Top contributors: {string.Join(", ", topNames)}.";
        }

        private static string FormatIndustryList(string question, JsonElement root, JsonElement indEl)
        {
            var yearBit = YearSuffix(root);
            var first = indEl.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Object)
            {
                return "No industry data found for the selected criteria.";
            }

            // Lost / won deal counts by industry
            if (first.TryGetProperty("dealCount", out _) ||
                (first.TryGetProperty("lostDeals", out _) && !first.TryGetProperty("revenue", out _) && !first.TryGetProperty("winRate", out _)))
            {
                var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
                var label = string.Equals(status, "Lost", StringComparison.OrdinalIgnoreCase) ? "lost deals"
                    : string.Equals(status, "Won", StringComparison.OrdinalIgnoreCase) ? "won deals"
                    : "deals";
                var top = first;
                var count = top.TryGetProperty("dealCount", out var dc) ? dc.GetInt32()
                    : string.Equals(status, "Lost", StringComparison.OrdinalIgnoreCase) ? GetInt(top, "lostDeals")
                    : GetInt(top, "wonDeals");
                var rows = indEl.EnumerateArray()
                    .Take(5)
                    .Select(e =>
                    {
                        var c = e.TryGetProperty("dealCount", out var d) ? d.GetInt32()
                            : string.Equals(status, "Lost", StringComparison.OrdinalIgnoreCase) ? GetInt(e, "lostDeals")
                            : GetInt(e, "wonDeals");
                        return $"{e.GetProperty("industry").GetString()} ({c:N0})";
                    })
                    .ToList();
                return $"{top.GetProperty("industry").GetString()} has the most {label}{yearBit} with {count:N0}. Ranking: {string.Join(", ", rows)}.";
            }

            // Win-rate ranking
            if (first.TryGetProperty("winRate", out _) ||
                question.Contains("win rate", StringComparison.OrdinalIgnoreCase) ||
                question.Contains("win-rate", StringComparison.OrdinalIgnoreCase))
            {
                var top = first;
                if (top.ValueKind == JsonValueKind.Object && top.TryGetProperty("winRate", out _))
                {
                    var won = top.TryGetProperty("wonDeals", out var w) ? w.GetInt32() : 0;
                    var total = top.TryGetProperty("totalDeals", out var t) ? t.GetInt32() : 0;
                    return $"{top.GetProperty("industry").GetString()} has the highest win rate{yearBit} at {GetDecimal(top, "winRate"):F1}% ({won} won / {total} closed).";
                }
            }

            // Revenue ranking
            if (first.TryGetProperty("revenue", out _))
            {
                var rows = indEl.EnumerateArray()
                    .Take(5)
                    .Select(e => $"{e.GetProperty("industry").GetString()} (${GetDecimal(e, "revenue"):N0})")
                    .ToList();
                return $"Industry revenue{yearBit}: {string.Join(", ", rows)}.";
            }

            return $"Industry breakdown{yearBit} calculated from your CRM data.";
        }

        private static int GetInt(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.TryGetInt32(out var i) ? i : 0;

        private static string FormatMonthList(JsonElement root, JsonElement monthEl)
        {
            var rows = monthEl.EnumerateArray()
                .Take(6)
                .Select(e => $"{e.GetProperty("month").GetString()}: ${GetDecimal(e, "revenue"):N0}")
                .ToList();
            return $"Monthly revenue: {string.Join(", ", rows)}.";
        }

        private static string FormatArray(string question, JsonElement root)
        {
            var rows = root.EnumerateArray().Take(5).ToList();
            if (rows.Count == 0) return "No data found for the selected criteria.";

            if (rows[0].TryGetProperty("correlation", out _) || rows[0].TryGetProperty("Correlation", out _))
            {
                var parts = rows.Select(r =>
                {
                    var factor = r.TryGetProperty("factor", out var f) ? f.GetString() : r.GetProperty("Factor").GetString();
                    var corr = r.TryGetProperty("correlation", out var c) ? c.GetDecimal() : r.GetProperty("Correlation").GetDecimal();
                    return $"{factor} ({corr:+#.##;-#.##;0})";
                }).ToList();
                return $"Factors correlated with win rate: {string.Join(", ", parts)}.";
            }

            if (rows[0].TryGetProperty("winRate", out _))
            {
                var top = rows[0];
                return $"{top.GetProperty("industry").GetString()} has the highest win rate at {GetDecimal(top, "winRate"):F1}%.";
            }

            if (rows[0].TryGetProperty("averageSalesCycleDays", out _))
            {
                var parts = rows.Select(r => $"{r.GetProperty("industry").GetString()} ({GetDecimal(r, "averageSalesCycleDays"):F1} days)").ToList();
                return $"Average sales cycle by industry: {string.Join(", ", parts)}.";
            }

            return "I couldn't summarize these results. Please try a more specific question.";
        }

        private static string YearSuffix(JsonElement root) =>
            root.TryGetProperty("year", out var yEl) && yEl.ValueKind != JsonValueKind.Null
                ? $" in {yEl.GetRawText()}"
                : string.Empty;

        private static decimal GetDecimal(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.TryGetDecimal(out var d) ? d : 0m;
    }
}
