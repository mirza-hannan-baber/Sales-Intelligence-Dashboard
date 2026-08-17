using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        private readonly ApplicationDbContext _db;

        public DataAnalysisService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<string>> GetAgentNamesAsync() =>
            await _db.Agents.AsNoTracking().Select(a => a.Name).Where(n => n != "").Distinct().ToListAsync();

        public async Task<List<string>> GetSectorsAsync() =>
            await _db.Deals.AsNoTracking().Select(d => d.Sector).Where(s => s != "").Distinct().ToListAsync();

        public async Task<object> ExecuteAnalysisAsync(AiIntent intent)
        {
            var deals = await _db.Deals.AsNoTracking().ToListAsync();
            var agents = await _db.Agents.AsNoTracking().ToListAsync();

            var scoped = ApplyFilters(deals, intent).ToList();
            var won = scoped.Where(d => d.Status == "Won").ToList();
            var lost = scoped.Where(d => d.Status == "Lost").ToList();
            var closed = scoped.Where(d => d.Status is "Won" or "Lost").ToList();

            var op = (intent.Operation ?? "summary").ToLowerInvariant();
            var groupBy = (intent.GroupBy ?? string.Empty).ToLowerInvariant();
            var target = (intent.TargetColumn ?? string.Empty).ToLowerInvariant();

            return op switch
            {
                "count_agents" => CountAgents(scoped, agents, intent.Year),
                "count" => CountDeals(scoped, won, lost, intent),
                "sum" or "yearly_revenue" => SumRevenue(won, intent),
                "monthly_revenue" => MonthlyRevenueBreakdown(won, intent),
                "group_sum" when groupBy is "sales_agent" or "employee" or "owner" => EmployeeRevenueBreakdown(won, intent.Year, topOnly: false),
                "group_sum" when groupBy is "industry" or "sector" => IndustryRevenueBreakdown(won, intent.Year),
                "group_count" when groupBy is "industry" or "sector" => IndustryDealCountBreakdown(scoped, intent, topOnly: true, ascending: false),
                "group_count_bottom" when groupBy is "industry" or "sector" => IndustryDealCountBreakdown(scoped, intent, topOnly: true, ascending: true),
                "group_count" => IndustryDealCountBreakdown(scoped, intent, topOnly: false, ascending: false),
                "employee_ranking" => EmployeeRevenueBreakdown(won, intent.Year, topOnly: true),
                "employee_ranking_bottom" => EmployeeRevenueBreakdownBottom(won, intent.Year),
                "win_rate" => WinRateAnalysis(closed, groupBy, intent.Year),
                "average" => AverageAnalysis(scoped, won, target, groupBy, intent),
                "max" => MaxAnalysis(won, closed, scoped, target, groupBy, intent),
                "min" => MinAnalysis(won, target, groupBy, intent),
                _ => Summary(scoped, won, lost, agents, intent)
            };
        }

        public async Task<List<CorrelationFactor>> CalculateCorrelationAsync()
        {
            var deals = await _db.Deals.AsNoTracking().Where(d => d.Status == "Won" || d.Status == "Lost").ToListAsync();

            if (deals.Count == 0)
            {
                return new List<CorrelationFactor>
                {
                    new() { Factor = "Sales Cycle Length", Correlation = 0 },
                    new() { Factor = "Deal Size", Correlation = 0 },
                    new() { Factor = "Revenue", Correlation = 0 }
                };
            }

            var isWon = deals.Select(d => d.Status == "Won" ? 1.0 : 0.0).ToList();
            var dealSizes = deals.Select(d => (double)d.Value).ToList();
            var cycleLengths = deals.Select(d => (double)d.DealDurationDays).ToList();

            return new List<CorrelationFactor>
            {
                new() { Factor = "Sales Cycle Length", Correlation = Math.Round(ComputePearsonCorrelation(cycleLengths, isWon), 2) },
                new() { Factor = "Deal Size", Correlation = Math.Round(ComputePearsonCorrelation(dealSizes, isWon), 2) },
                new() { Factor = "Revenue", Correlation = Math.Round(ComputePearsonCorrelation(dealSizes, isWon), 2) }
            };
        }

        public async Task<BusinessRecommendationInsights> GetRecommendationInsightsAsync()
        {
            var deals = await _db.Deals.AsNoTracking().ToListAsync();

            var industryStats = deals
                .GroupBy(d => string.IsNullOrWhiteSpace(d.Sector) ? "Unknown" : d.Sector)
                .Select(g =>
                {
                    var closed = g.Count(d => d.Status == "Won" || d.Status == "Lost");
                    var won = g.Count(d => d.Status == "Won");
                    double winRate = closed > 0 ? Math.Round((double)won / closed * 100, 1) : 0;
                    double avgCycle = g.Average(d => d.DealDurationDays);
                    return new { Industry = g.Key, WinRate = winRate, AvgCycle = avgCycle };
                })
                .ToList();

            var topIndustry = industryStats.OrderByDescending(i => i.WinRate).FirstOrDefault();
            var lowestIndustry = industryStats.OrderBy(i => i.WinRate).FirstOrDefault();
            var longestCycle = industryStats.OrderByDescending(i => i.AvgCycle).FirstOrDefault();
            var correlations = await CalculateCorrelationAsync();
            var strongest = correlations.OrderByDescending(c => Math.Abs(c.Correlation)).FirstOrDefault();

            return new BusinessRecommendationInsights
            {
                HighestWinRateIndustry = topIndustry?.Industry ?? "N/A",
                HighestWinRate = topIndustry?.WinRate ?? 0,
                LowestWinRateIndustry = lowestIndustry?.Industry ?? "N/A",
                LowestWinRate = lowestIndustry?.WinRate ?? 0,
                LongestSalesCycleIndustry = longestCycle?.Industry ?? "N/A",
                AverageSalesCycle = Math.Round(longestCycle?.AvgCycle ?? 0, 1),
                StrongestFactor = strongest?.Factor ?? "Sales Cycle Length",
                Correlation = strongest?.Correlation ?? 0
            };
        }

        private static IEnumerable<Deal> ApplyFilters(IEnumerable<Deal> deals, AiIntent intent)
        {
            var scoped = deals;

            if (intent.Year.HasValue)
            {
                scoped = scoped.Where(d => DealDate(d).Year == intent.Year.Value);
            }

            if (intent.Month.HasValue)
            {
                scoped = scoped.Where(d => DealDate(d).Month == intent.Month.Value);
            }

            if (!string.IsNullOrWhiteSpace(intent.FilterValue))
            {
                var col = (intent.FilterColumn ?? string.Empty).ToLowerInvariant();
                if (col is "industry" or "sector")
                {
                    scoped = scoped.Where(d =>
                        (d.Sector ?? string.Empty).Equals(intent.FilterValue, StringComparison.OrdinalIgnoreCase));
                }
                else if (col is "sales_agent" or "owner" or "employee")
                {
                    scoped = scoped.Where(d =>
                        (d.Owner ?? string.Empty).Equals(intent.FilterValue, StringComparison.OrdinalIgnoreCase));
                }
                else if (col is "status")
                {
                    scoped = scoped.Where(d =>
                        d.Status.Equals(intent.FilterValue, StringComparison.OrdinalIgnoreCase));
                }
            }

            return scoped;
        }

        private static object Summary(List<Deal> scoped, List<Deal> won, List<Deal> lost, List<Agent> agents, AiIntent intent) =>
            new
            {
                year = intent.Year,
                month = intent.Month,
                totalDeals = scoped.Count,
                wonDeals = won.Count,
                lostDeals = lost.Count,
                totalRevenue = won.Sum(d => d.Value),
                uniqueSalesAgents = agents.Count > 0 ? agents.Count : CountDistinctAgents(scoped),
                averageDealValue = won.Count > 0 ? Math.Round(won.Average(d => d.Value), 2) : 0m,
                note = "Summary calculated from CRM deals in scope."
            };

        private static object CountDeals(List<Deal> scoped, List<Deal> won, List<Deal> lost, AiIntent intent)
        {
            var target = (intent.TargetColumn ?? "deals").ToLowerInvariant();
            if (target is "sales_agent" or "employee" or "agent")
            {
                return CountAgents(scoped, new List<Agent>(), intent.Year);
            }

            return new
            {
                year = intent.Year,
                month = intent.Month,
                totalDeals = scoped.Count,
                wonDeals = won.Count,
                lostDeals = lost.Count,
                uniqueSalesAgents = CountDistinctAgents(scoped),
                filter = intent.FilterValue
            };
        }

        private static object SumRevenue(List<Deal> won, AiIntent intent) =>
            new
            {
                year = intent.Year,
                month = intent.Month,
                totalRevenue = won.Sum(d => d.Value),
                wonDealsCount = won.Count,
                uniqueSalesAgents = CountDistinctAgents(won),
                filter = intent.FilterValue,
                note = intent.Year.HasValue
                    ? $"Won-deal revenue for {intent.Year.Value}{(intent.Month.HasValue ? $"-{intent.Month.Value:00}" : string.Empty)}."
                    : "Won-deal revenue for all matching records."
            };

        private static object MonthlyRevenueBreakdown(List<Deal> won, AiIntent intent)
        {
            var rows = won
                .GroupBy(d => new { DealDate(d).Year, DealDate(d).Month })
                .Select(g => new
                {
                    month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    year = g.Key.Year,
                    monthNumber = g.Key.Month,
                    revenue = g.Sum(d => d.Value),
                    wonDeals = g.Count()
                })
                .OrderBy(r => r.year).ThenBy(r => r.monthNumber)
                .ToList();

            if (intent.Year.HasValue)
            {
                rows = rows.Where(r => r.year == intent.Year.Value).ToList();
            }

            return new
            {
                year = intent.Year,
                totalRevenue = rows.Sum(r => r.revenue),
                months = rows
            };
        }

        private static object IndustryRevenueBreakdown(List<Deal> won, int? year)
        {
            var rows = won
                .GroupBy(d => string.IsNullOrWhiteSpace(d.Sector) ? "Unknown" : d.Sector)
                .Select(g => new
                {
                    industry = g.Key,
                    revenue = g.Sum(d => d.Value),
                    wonDeals = g.Count()
                })
                .OrderByDescending(x => x.revenue)
                .ToList();

            return new
            {
                year,
                totalRevenue = rows.Sum(r => r.revenue),
                industries = rows
            };
        }

        private static object IndustryDealCountBreakdown(List<Deal> scoped, AiIntent intent, bool topOnly, bool ascending)
        {
            var status = intent.FilterValue;
            var rows = scoped
                .GroupBy(d => string.IsNullOrWhiteSpace(d.Sector) ? "Unknown" : d.Sector)
                .Select(g => new
                {
                    industry = g.Key,
                    dealCount = g.Count(),
                    lostDeals = g.Count(d => d.Status == "Lost"),
                    wonDeals = g.Count(d => d.Status == "Won"),
                    totalDeals = g.Count()
                })
                .ToList();

            rows = ascending
                ? rows.OrderBy(x => x.dealCount).ToList()
                : rows.OrderByDescending(x => x.dealCount).ToList();

            var top = rows.FirstOrDefault();
            var metricLabel = string.Equals(status, "Lost", StringComparison.OrdinalIgnoreCase) ? "lostDeals"
                : string.Equals(status, "Won", StringComparison.OrdinalIgnoreCase) ? "wonDeals"
                : "dealCount";

            if (topOnly)
            {
                return new
                {
                    year = intent.Year,
                    status,
                    metric = metricLabel,
                    topIndustry = top?.industry ?? "N/A",
                    dealCount = top?.dealCount ?? 0,
                    lostDeals = top?.lostDeals ?? 0,
                    wonDeals = top?.wonDeals ?? 0,
                    industries = rows
                };
            }

            return new
            {
                year = intent.Year,
                status,
                metric = metricLabel,
                industries = rows
            };
        }

        private static object WinRateAnalysis(List<Deal> closed, string groupBy, int? year)
        {
            if (groupBy is "industry" or "sector" or "")
            {
                var rows = closed
                    .GroupBy(d => string.IsNullOrWhiteSpace(d.Sector) ? "Unknown" : d.Sector)
                    .Select(g =>
                    {
                        var wonCount = g.Count(d => d.Status == "Won");
                        var total = g.Count();
                        double rate = total > 0 ? Math.Round((double)wonCount / total * 100, 1) : 0;
                        return new { industry = g.Key, totalDeals = total, wonDeals = wonCount, winRate = rate };
                    })
                    .OrderByDescending(g => g.winRate)
                    .ToList();

                var top = rows.FirstOrDefault();
                return new
                {
                    year,
                    topIndustry = top?.industry ?? "N/A",
                    winRate = top?.winRate ?? 0,
                    industries = rows
                };
            }

            var overallClosed = closed.Count;
            var overallWon = closed.Count(d => d.Status == "Won");
            var overallRate = overallClosed > 0 ? Math.Round((double)overallWon / overallClosed * 100, 1) : 0;
            return new { year, winRate = overallRate, totalDeals = overallClosed, wonDeals = overallWon };
        }

        private static object AverageAnalysis(List<Deal> scoped, List<Deal> won, string target, string groupBy, AiIntent intent)
        {
            var useWon = target is "revenue" or "deal_size" or "" or "value";
            var source = useWon ? won : scoped;

            if (groupBy is "industry" or "sector")
            {
                return source
                    .GroupBy(d => string.IsNullOrWhiteSpace(d.Sector) ? "Unknown" : d.Sector)
                    .Select(g => new
                    {
                        industry = g.Key,
                        averageSalesCycleDays = target.Contains("cycle")
                            ? Math.Round(g.Average(d => (double)d.DealDurationDays), 1)
                            : Math.Round((double)g.Average(d => d.Value), 2)
                    })
                    .OrderByDescending(g => g.averageSalesCycleDays)
                    .ToList();
            }

            var metric = target.Contains("cycle") ? "sales_cycle_days" : "deal_value";
            var avg = target.Contains("cycle")
                ? source.Select(d => (double)d.DealDurationDays).DefaultIfEmpty(0).Average()
                : (double)source.Select(d => d.Value).DefaultIfEmpty(0).Average();

            return new
            {
                year = intent.Year,
                month = intent.Month,
                metric,
                averageValue = Math.Round(avg, 2),
                recordCount = source.Count
            };
        }

        private static object MaxAnalysis(List<Deal> won, List<Deal> closed, List<Deal> scoped, string target, string groupBy, AiIntent intent)
        {
            if (target is "is_won" or "win_rate")
            {
                return WinRateAnalysis(closed, "industry", intent.Year);
            }

            if (groupBy is "industry" or "sector")
            {
                var isLost = string.Equals(intent.FilterValue, "Lost", StringComparison.OrdinalIgnoreCase);
                var isWon = string.Equals(intent.FilterValue, "Won", StringComparison.OrdinalIgnoreCase);
                if (isLost || isWon || target is "deals" or "deal_count" or "count")
                {
                    return IndustryDealCountBreakdown(scoped, intent, topOnly: true, ascending: false);
                }

                return IndustryRevenueBreakdown(won, intent.Year);
            }

            var topDeal = won.OrderByDescending(d => d.Value).FirstOrDefault();
            return new
            {
                year = intent.Year,
                topDeal = topDeal?.Name ?? "N/A",
                owner = topDeal?.Owner ?? "N/A",
                revenue = topDeal?.Value ?? 0,
                industry = topDeal?.Sector ?? "N/A"
            };
        }

        private static object MinAnalysis(List<Deal> won, string target, string groupBy, AiIntent intent)
        {
            var bottomDeal = won.Where(d => d.Value > 0).OrderBy(d => d.Value).FirstOrDefault();
            return new
            {
                year = intent.Year,
                deal = bottomDeal?.Name ?? "N/A",
                owner = bottomDeal?.Owner ?? "N/A",
                revenue = bottomDeal?.Value ?? 0
            };
        }

        private static object CountAgents(List<Deal> scopedDeals, List<Agent> agents, int? year)
        {
            int unique = year.HasValue
                ? CountDistinctAgents(scopedDeals)
                : (agents.Count > 0 ? agents.Count : CountDistinctAgents(scopedDeals));

            return new
            {
                uniqueSalesAgents = unique,
                year,
                totalDeals = scopedDeals.Count,
                note = year.HasValue
                    ? $"Distinct sales agents on deals in {year.Value}."
                    : "Unique sales agents in the CRM, not the number of deal records."
            };
        }

        private static object EmployeeRevenueBreakdown(List<Deal> wonDeals, int? year, bool topOnly)
        {
            var rows = wonDeals
                .Where(d => !string.IsNullOrWhiteSpace(d.Owner))
                .GroupBy(d => d.Owner, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    salesAgent = g.Key,
                    revenue = g.Sum(d => d.Value),
                    wonDeals = g.Count()
                })
                .OrderByDescending(x => x.revenue)
                .ToList();

            var total = rows.Sum(r => r.revenue);
            var top = rows.FirstOrDefault();

            if (topOnly)
            {
                return new
                {
                    year,
                    topEmployee = top?.salesAgent ?? "N/A",
                    revenue = top?.revenue ?? 0,
                    wonDeals = top?.wonDeals ?? 0,
                    uniqueSalesAgents = rows.Count,
                    totalRevenue = total,
                    employees = rows
                };
            }

            return new { year, uniqueSalesAgents = rows.Count, totalRevenue = total, employees = rows };
        }

        private static object EmployeeRevenueBreakdownBottom(List<Deal> wonDeals, int? year)
        {
            var rows = wonDeals
                .Where(d => !string.IsNullOrWhiteSpace(d.Owner))
                .GroupBy(d => d.Owner, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    salesAgent = g.Key,
                    revenue = g.Sum(d => d.Value),
                    wonDeals = g.Count()
                })
                .OrderBy(x => x.revenue)
                .ToList();

            var bottom = rows.FirstOrDefault();
            return new
            {
                year,
                bottomEmployee = bottom?.salesAgent ?? "N/A",
                revenue = bottom?.revenue ?? 0,
                wonDeals = bottom?.wonDeals ?? 0,
                uniqueSalesAgents = rows.Count,
                totalRevenue = rows.Sum(r => r.revenue),
                employees = rows
            };
        }

        private static DateTime DealDate(Deal d) => d.CloseDate ?? d.CreatedDate;

        private static int CountDistinctAgents(IEnumerable<Deal> deals) =>
            deals.Select(d => d.Owner).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        private static double ComputePearsonCorrelation(List<double> x, List<double> y)
        {
            int n = Math.Min(x.Count, y.Count);
            if (n < 2) return 0;

            double avgX = x.Average();
            double avgY = y.Average();
            double sumXY = 0, sumX2 = 0, sumY2 = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = x[i] - avgX;
                double dy = y[i] - avgY;
                sumXY += dx * dy;
                sumX2 += dx * dx;
                sumY2 += dy * dy;
            }

            double denom = Math.Sqrt(sumX2 * sumY2);
            return denom == 0 ? 0 : sumXY / denom;
        }
    }
}
