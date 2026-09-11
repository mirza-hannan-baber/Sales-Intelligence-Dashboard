using System.Text.RegularExpressions;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    /// <summary>
    /// Deterministic overlay on top of LLM intent classification.
    /// Prevents the LLM from routing numerical questions to the wrong aggregation.
    /// </summary>
    public static class IntentRefiner
    {
        public static AiIntent Refine(string question, AiIntent intent)
        {
            intent ??= new AiIntent();
            var q = (question ?? string.Empty).ToLowerInvariant();

            var yearMatch = Regex.Match(question ?? string.Empty, @"\b(20[0-9]{2})\b");
            if (yearMatch.Success && !intent.Year.HasValue)
            {
                intent.Year = int.Parse(yearMatch.Groups[1].Value);
            }

            if (IsForecastQuestion(q))
            {
                intent.Intent = "forecast";
                intent.ForecastType = q.Contains("quarter") ? "next_quarter" : "next_3_months";
                return intent;
            }

            if (IsCorrelationQuestion(q))
            {
                intent.Intent = "correlation_analysis";
                intent.TargetColumn = "is_won";
                intent.CompareColumns = new List<string> { "deal_size", "sales_cycle_length", "revenue" };
                return intent;
            }

            if (IsRecommendationQuestion(q))
            {
                intent.Intent = "business_recommendation";
                return intent;
            }

            if (IsTopEmployeeQuestion(q))
            {
                intent.Intent = "data_analysis";
                intent.Operation = "employee_ranking";
                intent.GroupBy = "sales_agent";
                intent.TargetColumn = "revenue";
                return intent;
            }

            if (IsLostDealsByIndustryQuestion(q))
            {
                intent.Intent = "data_analysis";
                intent.Operation = "group_count";
                intent.GroupBy = "industry";
                intent.TargetColumn = "deals";
                intent.FilterColumn = "status";
                intent.FilterValue = "Lost";
                return intent;
            }

            if (IsAgentCountQuestion(q))
            {
                intent.Intent = "data_analysis";
                intent.Operation = "count_agents";
                intent.TargetColumn = "sales_agent";
                return intent;
            }

            if (IsEmployeeBreakdownQuestion(q))
            {
                intent.Intent = "data_analysis";
                intent.Operation = "group_sum";
                intent.GroupBy = "sales_agent";
                intent.TargetColumn = "revenue";
                return intent;
            }

            if (IsWinRateQuestion(q))
            {
                intent.Intent = "data_analysis";
                intent.Operation = "win_rate";
                intent.GroupBy = "industry";
                intent.TargetColumn = "is_won";
                // Comparative win-rate questions must not filter to a named industry.
                if (q.Contains("highest") || q.Contains("lowest") || q.Contains("which industry") || q.Contains("which sector"))
                {
                    if (string.Equals(intent.FilterColumn, "industry", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(intent.FilterColumn, "sector", StringComparison.OrdinalIgnoreCase))
                    {
                        intent.FilterColumn = null;
                        intent.FilterValue = null;
                    }
                }
                return intent;
            }

            if (IsRevenueQuestion(q))
            {
                intent.Intent = "data_analysis";
                intent.Operation = intent.Year.HasValue ? "yearly_revenue" : "sum";
                intent.TargetColumn = "revenue";
                return intent;
            }

            if (string.Equals(intent.Intent, "general", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(intent.Intent, "off_topic", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(intent.Intent))
            {
                if (LooksLikeSalesQuestion(q))
                {
                    intent.Intent = "data_analysis";
                    intent.Operation = string.IsNullOrWhiteSpace(intent.Operation) ? "sum" : intent.Operation;
                    intent.TargetColumn = string.IsNullOrWhiteSpace(intent.TargetColumn) ? "revenue" : intent.TargetColumn;
                }
            }

            return intent;
        }

        private static bool IsForecastQuestion(string q)
        {
            if (q.Contains("202") && !q.Contains("next") && !q.Contains("forecast") && !q.Contains("predict"))
            {
                return false;
            }

            return q.Contains("forecast")
                || q.Contains("next quarter")
                || q.Contains("next month")
                || (q.Contains("predict") && q.Contains("revenue") && !q.Contains("employee"));
        }

        private static bool IsCorrelationQuestion(string q)
        {
            return q.Contains("influence")
                || q.Contains("factor")
                || q.Contains("correlation")
                || q.Contains("affect win")
                || q.Contains("what drives");
        }

        private static bool IsRecommendationQuestion(string q)
        {
            return q.Contains("increase my sales")
                || q.Contains("how can i increase")
                || q.Contains("recommend")
                || q.Contains("advice")
                || q.Contains("how can we improve");
        }

        private static bool IsAgentCountQuestion(string q)
        {
            var mentionsAgent = q.Contains("agent") || q.Contains("employee") || q.Contains("rep");
            if (!mentionsAgent) return false;
            if (q.Contains("revenue") || q.Contains("generated") || q.Contains("highest") || q.Contains("top")
                || q.Contains("best") || q.Contains("perform") || q.Contains("worst") || q.Contains("lowest"))
            {
                return false;
            }

            return q.Contains("how many")
                || q.Contains("number of")
                || q.Contains("count")
                || q.Contains("how much sales agent")
                || q.Contains("how much sales agents")
                || (q.Contains("how much") && mentionsAgent && !q.Contains("revenue"));
        }

        private static bool IsTopEmployeeQuestion(string q)
        {
            var mentionsPerson = q.Contains("employee") || q.Contains("agent") || q.Contains("rep");
            var mentionsTop = q.Contains("highest") || q.Contains("top") || q.Contains("most") || q.Contains("best");
            var mentionsMetric = q.Contains("revenue") || q.Contains("sales") || q.Contains("perform")
                || q.Contains("performance") || q.Contains("won") || q.Contains("deal");
            return mentionsPerson && mentionsTop && (mentionsMetric || !q.Contains("how many"));
        }

        private static bool IsLostDealsByIndustryQuestion(string q)
        {
            var mentionsIndustry = q.Contains("industry") || q.Contains("sector");
            var mentionsLost = q.Contains("lost");
            var mentionsDeal = q.Contains("deal");
            var asksMost = q.Contains("most") || q.Contains("highest") || q.Contains("top") || q.Contains("which");
            return mentionsIndustry && mentionsLost && mentionsDeal && asksMost;
        }

        private static bool IsEmployeeBreakdownQuestion(string q)
        {
            var mentionsPerson = q.Contains("employee") || q.Contains("agent") || q.Contains("rep");
            return mentionsPerson && (
                q.Contains("each")
                || q.Contains("per employee")
                || q.Contains("per agent")
                || q.Contains("by employee")
                || q.Contains("by agent")
                || q.Contains("breakdown")
                || q.Contains("list"));
        }

        private static bool IsWinRateQuestion(string q)
        {
            return q.Contains("win rate") || q.Contains("win-rate");
        }

        private static bool IsRevenueQuestion(string q)
        {
            return q.Contains("revenue") || (q.Contains("sales") && q.Contains("generat"));
        }

        private static bool LooksLikeSalesQuestion(string q)
        {
            return q.Contains("revenue")
                || q.Contains("sales")
                || q.Contains("deal")
                || q.Contains("agent")
                || q.Contains("employee")
                || q.Contains("industry")
                || q.Contains("win rate");
        }
    }
}
