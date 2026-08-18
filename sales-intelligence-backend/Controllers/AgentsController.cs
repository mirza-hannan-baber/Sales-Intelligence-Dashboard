using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Services;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmployeeFeatureService _features;

        public AgentsController(ApplicationDbContext db, IEmployeeFeatureService features)
        {
            _db = db;
            _features = features;
        }

        [HttpGet]
        public async Task<IActionResult> GetAgents([FromQuery] string? search, [FromQuery] string? department, [FromQuery] string? regionalOffice)
        {
            var query = _db.Agents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(s) || a.Email.ToLower().Contains(s) || (a.RegionalOffice != null && a.RegionalOffice.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(department) && !department.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Department == department);
            }

            if (!string.IsNullOrWhiteSpace(regionalOffice) && !regionalOffice.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.RegionalOffice == regionalOffice);
            }

            var agents = (await query.ToListAsync())
                .OrderByDescending(a => a.TotalRevenue)
                .ToList();

            // Recompute the performance score live from the deal ledger so the table
            // never serves a stale seeded value.
            var winRates = await _features.GetWinRatePercentByAgentAsync();
            foreach (var a in agents)
            {
                if (winRates.TryGetValue(a.Name, out var wr))
                {
                    a.WinRate = wr;
                    a.PerformanceScore = wr;
                }
            }

            var totalAgents = agents.Count;
            var avgRevenue = totalAgents > 0 ? agents.Average(a => (double)a.TotalRevenue) : 0;
            var avgWinRate = totalAgents > 0 ? agents.Average(a => a.WinRate) : 0;
            var topPerformer = agents.OrderByDescending(a => a.PerformanceScore).FirstOrDefault();
            var regionalOffices = await _db.Agents.Where(a => !string.IsNullOrEmpty(a.RegionalOffice)).Select(a => a.RegionalOffice).Distinct().OrderBy(r => r).ToListAsync();

            return Ok(new
            {
                totalAgents,
                avgRevenue = Math.Round(avgRevenue),
                avgWinRate = Math.Round(avgWinRate, 1),
                topPerformerName = topPerformer?.Name ?? "N/A",
                topPerformerScore = topPerformer?.PerformanceScore ?? 0,
                performanceScoreBasis = "win_rate_percentage",
                filterOptions = new { regionalOffices },
                items = agents
            });
        }

        [HttpGet("{id:int}/performance")]
        public async Task<IActionResult> GetAgentPerformance(int id)
        {
            var agent = await _db.Agents.FindAsync(id);
            if (agent == null) return NotFound();

            var deals = await _db.Deals.Where(d => d.Owner == agent.Name).ToListAsync();
            var won = deals.Where(d => d.Status == "Won").ToList();
            var closed = deals.Where(d => d.Status is "Won" or "Lost").ToList();

            // Build last-12-month revenue for lag features (anchored to latest deal date)
            var end = deals.Select(d => d.CloseDate ?? d.CreatedDate).DefaultIfEmpty(DateTime.UtcNow).Max().Date;
            var start = new DateTime(end.Year, end.Month, 1).AddMonths(-11);
            var monthly = Enumerable.Range(0, 12)
                .Select(i => start.AddMonths(i))
                .Select(m =>
                {
                    var monthWon = won.Where(d =>
                    {
                        var date = d.CloseDate ?? d.CreatedDate;
                        return date.Year == m.Year && date.Month == m.Month;
                    }).ToList();
                    return monthWon.Sum(d => d.Value);
                })
                .ToList();

            decimal Lag(int monthsBack) =>
                monthly.Count >= monthsBack ? monthly[^monthsBack] : 0;

            var lag1 = Lag(1);
            var lag2 = Lag(2);
            var lag3 = Lag(3);
            var lag6 = Lag(6);
            var lag12 = Lag(12);

            var last3 = monthly.TakeLast(3).Where(v => v > 0).ToList();
            var last6 = monthly.TakeLast(6).Where(v => v > 0).ToList();
            var last12 = monthly.Where(v => v > 0).ToList();

            var recentClosed = closed
                .OrderByDescending(d => d.CloseDate ?? d.CreatedDate)
                .Take(Math.Max(1, closed.Count))
                .ToList();
            var recentWon = recentClosed.Count(d => d.Status == "Won");
            var winRateLag1 = recentClosed.Count > 0
                ? Math.Round(recentWon * 100.0 / recentClosed.Count, 1)
                : agent.WinRate;

            var avgDeal = won.Count > 0 ? (double)won.Average(d => d.Value) : 0;
            var avgCycle = won.Where(d => d.DealDurationDays > 0).Select(d => d.DealDurationDays).DefaultIfEmpty(30).Average();

            // Recreate the same quarterly/yearly aggregations used by the training notebook.
            var quarterStart = new DateTime(end.Year, ((end.Month - 1) / 3) * 3 + 1, 1);
            var quarterEnd = quarterStart.AddMonths(3);
            var quarterDeals = deals
                .Where(d => d.CreatedDate >= quarterStart && d.CreatedDate < quarterEnd)
                .ToList();
            var quarterWon = won
                .Where(d =>
                {
                    var date = d.CloseDate ?? d.CreatedDate;
                    return date >= quarterStart && date < quarterEnd;
                })
                .ToList();
            var quarterRevenue = quarterWon.Sum(d => d.Value);
            var quarterWinRate = quarterDeals.Count > 0
                ? quarterDeals.Count(d => d.Status == "Won") * 100.0 / quarterDeals.Count
                : 0.0;
            var quarterAvgDeal = quarterDeals.Count > 0
                ? (double)quarterDeals.Average(d => d.ProposedValue)
                : 0.0;
            var quarterAvgCycle = quarterDeals
                .Where(d => d.DealDurationDays > 0)
                .Select(d => d.DealDurationDays)
                .DefaultIfEmpty(0)
                .Average();

            var yearDeals = deals.Where(d => d.CreatedDate.Year == end.Year).ToList();
            var annualRevenue = won
                .Where(d => (d.CloseDate ?? d.CreatedDate).Year == end.Year)
                .Sum(d => d.Value);
            var annualWinRate = yearDeals.Count > 0
                ? yearDeals.Count(d => d.Status == "Won") * 100.0 / yearDeals.Count
                : 0.0;
            var dealDates = deals.Select(d => d.CloseDate ?? d.CreatedDate).ToList();
            double yearsActive = dealDates.Count > 0
                ? Math.Max(0.0, end.Year - deals.Min(d => d.CreatedDate).Year)
                : 0.0;

            // Training-consistent model inputs for the latest COMPLETE period. These are
            // what the prediction endpoints actually use; the legacy lag fields below are
            // kept only so existing UI panels keep rendering.
            var yearly = await _features.GetYearlyFeaturesAsync(agent.Name);
            var quarterly = await _features.GetQuarterlyFeaturesAsync(agent.Name);

            var liveWinRates = await _features.GetWinRatePercentByAgentAsync();
            if (liveWinRates.TryGetValue(agent.Name, out var liveWinRate))
            {
                agent.WinRate = liveWinRate;
                agent.PerformanceScore = liveWinRate;
            }

            return Ok(new
            {
                agent,
                dealsCount = deals.Count,
                wonDealsCount = won.Count,
                deals,
                predictionFeatures = new
                {
                    salesAgent = agent.Name,
                    lag1,
                    lag2,
                    lag3,
                    lag6,
                    lag12,
                    rollingMean3 = last3.Count > 0 ? last3.Average() : 0,
                    rollingMean6 = last6.Count > 0 ? last6.Average() : 0,
                    rollingMean12 = last12.Count > 0 ? last12.Average() : 0,
                    totalDealsLag1 = recentClosed.Count,
                    wonDealsLag1 = recentWon,
                    closedDealsLag1 = recentClosed.Count,
                    totalRevenueLag1 = lag1,
                    averageDealValueLag1 = avgDeal,
                    averageSalesCycleLag1 = avgCycle,
                    winRateLag1,
                    winRateRolling3 = agent.WinRate,
                    revenueRolling3 = last3.Count > 0 ? last3.Average() : 0,
                    dealsRolling3 = Math.Round(deals.Count / 3.0, 1),
                    monthNumber = end.Month,
                    quarter = (end.Month - 1) / 3 + 1,
                    year = end.Year,

                    // ---- Model true features (latest complete period) ----
                    // Quarterly (employee performance -> next-quarter revenue).
                    // win_rate is a FRACTION in [0,1], matching how the model was trained.
                    quarterlyPeriod = quarterly?.Quarter,
                    dealsWorked = quarterly?.DealsWorked ?? quarterDeals.Count,
                    winRate = quarterly?.WinRate ?? quarterWinRate / 100.0,
                    avgDealSize = quarterly?.AvgDealSize ?? quarterAvgDeal,
                    avgCycleDays = quarterly?.AvgCycleDays ?? quarterAvgCycle,
                    revenue = quarterly?.Revenue ?? (double)quarterRevenue,
                    // Yearly (employee revenue -> next-year revenue)
                    yearlyPeriod = yearly?.Year,
                    revenueAnnual = yearly?.Revenue ?? (double)annualRevenue,
                    yearsActive = yearly?.YearsActive ?? yearsActive,
                    dealsWorkedAnnual = yearly?.DealsWorked ?? yearDeals.Count,
                    winRateAnnual = yearly?.WinRate ?? annualWinRate / 100.0
                }
            });
        }
    }
}
