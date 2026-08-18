using System.Globalization;
using System.Text.RegularExpressions;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    /// <summary>
    /// Maps natural-language data questions to deterministic query plans.
    /// </summary>
    public static class DataQuestionParser
    {
        private static readonly Dictionary<string, int> MonthNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["january"] = 1, ["jan"] = 1,
            ["february"] = 2, ["feb"] = 2,
            ["march"] = 3, ["mar"] = 3,
            ["april"] = 4, ["apr"] = 4,
            ["may"] = 5,
            ["june"] = 6, ["jun"] = 6,
            ["july"] = 7, ["jul"] = 7,
            ["august"] = 8, ["aug"] = 8,
            ["september"] = 9, ["sep"] = 9, ["sept"] = 9,
            ["october"] = 10, ["oct"] = 10,
            ["november"] = 11, ["nov"] = 11,
            ["december"] = 12, ["dec"] = 12
        };

        public static AiIntent Parse(string question, IEnumerable<string>? agentNames = null, IEnumerable<string>? sectors = null)
        {
            var q = (question ?? string.Empty).Trim();
            var lower = q.ToLowerInvariant();
            var intent = new AiIntent { Intent = "data_analysis" };

            ExtractYearMonth(q, intent);
            ExtractStatus(lower, intent);
            ExtractEntityFilters(q, lower, intent, agentNames, sectors);
            InferOperation(lower, intent);

            return intent;
        }

        public static bool IsDataQuestion(string question)
        {
            var lower = (question ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(lower)) return false;

            if (IsForecastQuestion(lower) || IsCorrelationQuestion(lower) || IsRecommendationQuestion(lower))
            {
                return false;
            }

            return ContainsAny(lower,
                "revenue", "sales", "deal", "deals", "agent", "agents", "employee", "employees", "rep", "reps",
                "industry", "sector", "win rate", "win-rate", "won", "lost", "pipeline", "forecast",
                "how many", "how much", "total", "count", "number of", "average", "avg", "mean",
                "highest", "lowest", "top", "bottom", "most", "least", "best", "worst",
                "each", "per ", "by employee", "by agent", "by industry", "breakdown", "list", "show me",
                "generate", "generated", "closed", "cycle", "probability", "compare", "sum", "maximum", "minimum");
        }

        public static bool IsForecastQuestion(string lower) =>
            (lower.Contains("forecast") || lower.Contains("next quarter") || lower.Contains("next month") ||
             (lower.Contains("predict") && lower.Contains("revenue") && !lower.Contains("employee")))
            && !(Regex.IsMatch(lower, @"\b20[0-9]{2}\b") && !lower.Contains("next") && !lower.Contains("forecast"));

        public static bool IsCorrelationQuestion(string lower) =>
            ContainsAny(lower, "influence", "factor", "correlation", "affect win", "what drives", "impact on win");

        public static bool IsRecommendationQuestion(string lower) =>
            ContainsAny(lower, "increase my sales", "how can i increase", "how can we improve", "recommend", "advice", "improve sales");

        private static void ExtractYearMonth(string question, AiIntent intent)
        {
            var yearMatch = Regex.Match(question, @"\b(20[0-9]{2})\b");
            if (yearMatch.Success)
            {
                intent.Year = int.Parse(yearMatch.Groups[1].Value);
            }

            foreach (var kv in MonthNames)
            {
                if (Regex.IsMatch(question, $@"\b{Regex.Escape(kv.Key)}\b", RegexOptions.IgnoreCase))
                {
                    intent.Month = kv.Value;
                    break;
                }
            }
        }

        private static void ExtractStatus(string lower, AiIntent intent)
        {
            if (Regex.IsMatch(lower, @"\bwon\b|\bclosed won\b"))
            {
                intent.FilterColumn = "status";
                intent.FilterValue = "Won";
            }
            else if (Regex.IsMatch(lower, @"\blost\b|\bclosed lost\b"))
            {
                intent.FilterColumn = "status";
                intent.FilterValue = "Lost";
            }
            else if (lower.Contains("in progress") || lower.Contains("open deal") || lower.Contains("pipeline"))
            {
                intent.FilterColumn = "status";
                intent.FilterValue = "In Progress";
            }
        }

        private static void ExtractEntityFilters(
            string question,
            string lower,
            AiIntent intent,
            IEnumerable<string>? agentNames,
            IEnumerable<string>? sectors)
        {
            // Comparative / ranking questions mention an industry or agent as a candidate,
            // not as a filter. Filtering them would wrongly claim that candidate is "highest".
            var isComparative = ContainsAny(lower,
                "highest", "lowest", "top", "bottom", "best", "worst", "most", "least",
                "which industry", "which sector", "which employee", "which agent",
                "who perform", "who performed", "perform best", "performed best");

            if (!isComparative && agentNames != null)
            {
                foreach (var name in agentNames.OrderByDescending(n => n.Length))
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (lower.Contains(name.ToLowerInvariant()))
                    {
                        intent.FilterColumn = "sales_agent";
                        intent.FilterValue = name;
                        break;
                    }
                }
            }

            if (!isComparative && string.IsNullOrWhiteSpace(intent.FilterValue) && sectors != null)
            {
                foreach (var sector in sectors.OrderByDescending(s => s.Length))
                {
                    if (string.IsNullOrWhiteSpace(sector)) continue;
                    if (lower.Contains(sector.ToLowerInvariant()))
                    {
                        intent.FilterColumn = "industry";
                        intent.FilterValue = sector;
                        break;
                    }
                }
            }

            if (ContainsAny(lower, "by employee", "by agent", "per employee", "per agent", "each employee", "each agent"))
            {
                intent.GroupBy = "sales_agent";
            }
            else if (ContainsAny(lower, "by industry", "per industry", "each industry", "by sector", "per sector"))
            {
                intent.GroupBy = "industry";
            }
            else if (ContainsAny(lower, "by month", "monthly", "per month", "each month"))
            {
                intent.GroupBy = "month";
            }
            else if (ContainsAny(lower, "by year", "yearly", "per year", "each year"))
            {
                intent.GroupBy = "year";
            }
        }

        private static void InferOperation(string lower, AiIntent intent)
        {
            var mentionsAgent = ContainsAny(lower, "agent", "employee", "rep");
            var mentionsRevenue = ContainsAny(lower, "revenue", "sales", "generated", "earn", "earning");
            var mentionsDeal = ContainsAny(lower, "deal", "deals", "opportunity", "opportunities");
            var mentionsIndustry = ContainsAny(lower, "industry", "industries", "sector", "sectors");
            var mentionsWinRate = lower.Contains("win rate") || lower.Contains("win-rate") || lower.Contains("winrate");
            var mentionsCycle = ContainsAny(lower, "sales cycle", "cycle length", "cycle days");
            var mentionsPerformance = ContainsAny(lower, "perform", "performance", "performer");
            var isLostFilter = string.Equals(intent.FilterValue, "Lost", StringComparison.OrdinalIgnoreCase);
            var isWonFilter = string.Equals(intent.FilterValue, "Won", StringComparison.OrdinalIgnoreCase);
            var asksCount = ContainsAny(lower, "how many", "number of", "count", "how much sales agent", "how much sales agents")
                || (lower.Contains("how much") && mentionsAgent && !mentionsRevenue);
            var asksTop = ContainsAny(lower, "highest", "top", "most", "best", "maximum", "max ");
            var asksBottom = ContainsAny(lower, "lowest", "least", "bottom", "worst", "minimum", "min ");
            var asksBreakdown = ContainsAny(lower, "each", "per ", "breakdown", "list", "show me", "by employee", "by agent", "by industry", "by month", "by year");
            var asksAverage = ContainsAny(lower, "average", "avg", "mean");
            var asksTotal = ContainsAny(lower, "total", "sum", "how much revenue", "how much did", "how much sales");

            if (mentionsWinRate && (asksTop || lower.Contains("which industry") || lower.Contains("which sector") || lower.Contains("highest")))
            {
                intent.Operation = "win_rate";
                intent.GroupBy ??= "industry";
                intent.TargetColumn = "is_won";
                ClearEntityFilter(intent);
                return;
            }

            if (mentionsWinRate)
            {
                intent.Operation = "win_rate";
                intent.TargetColumn = "is_won";
                if (mentionsIndustry) intent.GroupBy ??= "industry";
                return;
            }

            // Lost / won deal rankings by industry (e.g. "which industry has the most lost deals?")
            if ((asksTop || asksBottom) && mentionsIndustry && mentionsDeal && (isLostFilter || isWonFilter || ContainsAny(lower, "lost", "won")))
            {
                intent.Operation = asksBottom ? "group_count_bottom" : "group_count";
                intent.GroupBy = "industry";
                intent.TargetColumn = "deals";
                if (ContainsAny(lower, "lost") && string.IsNullOrWhiteSpace(intent.FilterValue))
                {
                    intent.FilterColumn = "status";
                    intent.FilterValue = "Lost";
                }
                else if (ContainsAny(lower, "won") && !mentionsWinRate && string.IsNullOrWhiteSpace(intent.FilterValue))
                {
                    intent.FilterColumn = "status";
                    intent.FilterValue = "Won";
                }
                ClearEntityFilter(intent, keepStatus: true);
                return;
            }

            // Best / top performing employee — rank by won revenue (not agent count)
            if ((asksTop || asksBottom) && mentionsAgent &&
                (mentionsRevenue || mentionsPerformance || mentionsDeal || !asksCount))
            {
                intent.Operation = asksBottom ? "employee_ranking_bottom" : "employee_ranking";
                intent.GroupBy = "sales_agent";
                intent.TargetColumn = "revenue";
                ClearEntityFilter(intent, keepStatus: true);
                return;
            }

            if (asksCount && mentionsAgent && !mentionsRevenue && !mentionsPerformance && !asksTop && !asksBottom)
            {
                intent.Operation = "count_agents";
                intent.TargetColumn = "sales_agent";
                return;
            }

            if (asksCount && (mentionsDeal || !mentionsRevenue))
            {
                intent.Operation = "count";
                intent.TargetColumn = mentionsDeal ? "deals" : intent.TargetColumn ?? "deals";
                return;
            }

            if (asksBreakdown && mentionsAgent)
            {
                intent.Operation = "group_sum";
                intent.GroupBy = "sales_agent";
                intent.TargetColumn = "revenue";
                return;
            }

            if (asksBreakdown && mentionsIndustry && (isLostFilter || ContainsAny(lower, "lost deal", "lost deals")))
            {
                intent.Operation = "group_count";
                intent.GroupBy = "industry";
                intent.TargetColumn = "deals";
                intent.FilterColumn = "status";
                intent.FilterValue = "Lost";
                return;
            }

            if (asksBreakdown && mentionsIndustry)
            {
                intent.Operation = "group_sum";
                intent.GroupBy = "industry";
                intent.TargetColumn = "revenue";
                return;
            }

            if (asksBreakdown && intent.GroupBy == "month")
            {
                intent.Operation = "monthly_revenue";
                intent.TargetColumn = "revenue";
                return;
            }

            if (mentionsCycle || asksAverage)
            {
                intent.Operation = "average";
                intent.TargetColumn = mentionsCycle ? "sales_cycle_length" : "deal_size";
                if (mentionsIndustry) intent.GroupBy = "industry";
                return;
            }

            if (asksTop && mentionsIndustry)
            {
                // Default industry "highest" without win-rate wording → revenue ranking
                intent.Operation = "group_sum";
                intent.GroupBy = "industry";
                intent.TargetColumn = "revenue";
                ClearEntityFilter(intent, keepStatus: true);
                return;
            }

            if (intent.Year.HasValue && intent.Month.HasValue && mentionsRevenue)
            {
                intent.Operation = "sum";
                intent.TargetColumn = "revenue";
                return;
            }

            if (intent.Year.HasValue && mentionsRevenue)
            {
                intent.Operation = "yearly_revenue";
                intent.TargetColumn = "revenue";
                return;
            }

            if (asksTotal || mentionsRevenue)
            {
                intent.Operation = "sum";
                intent.TargetColumn = "revenue";
                return;
            }

            if (mentionsDeal)
            {
                intent.Operation = "count";
                intent.TargetColumn = "deals";
                return;
            }

            if (mentionsAgent)
            {
                intent.Operation = "count_agents";
                intent.TargetColumn = "sales_agent";
                return;
            }

            if (mentionsIndustry)
            {
                intent.Operation = "win_rate";
                intent.GroupBy = "industry";
                return;
            }

            intent.Operation = "summary";
            intent.TargetColumn = "revenue";
        }

        private static void ClearEntityFilter(AiIntent intent, bool keepStatus = false)
        {
            var col = (intent.FilterColumn ?? string.Empty).ToLowerInvariant();
            if (keepStatus && col == "status") return;
            if (col is "industry" or "sector" or "sales_agent" or "owner" or "employee")
            {
                intent.FilterColumn = null;
                intent.FilterValue = null;
            }
        }

        private static bool ContainsAny(string text, params string[] terms) =>
            terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

}
