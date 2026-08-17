using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.DTOs;
using SalesIntelligence.Api.Utilities;

namespace SalesIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext db, ILogger<DashboardController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>One calendar month of settled CRM activity.</summary>
        private sealed record MonthBucket(
            DateTime Start,
            decimal WonRevenue,
            int ClosedCount,
            int WonCount)
        {
            public string Label => Start.ToString("MMM yyyy");
            public double WinRate => ClosedCount > 0
                ? Math.Round(WonCount * 100.0 / ClosedCount, 1)
                : 0;
        }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis([FromQuery] int months = 6)
        {
            months = months is 12 or 6 ? months : 6;

            var deals = await _db.Deals.AsNoTracking().ToListAsync();
            var wonDeals = deals.Where(d => d.Status == "Won").ToList();
            var closedDeals = deals.Where(d => d.Status is "Won" or "Lost").ToList();
            var openDeals = deals.Where(d => d.Status is "In Progress" or "Negotiation" or "At Risk").ToList();

            var totalRevenueVal = wonDeals.Sum(d => d.Value);
            var pipelineVal = openDeals.Sum(d => d.Value);
            var wonCount = wonDeals.Count;
            var closedCount = closedDeals.Count;
            double winRate = closedCount > 0
                ? Math.Round((double)wonCount / closedCount * 100, 1)
                : 0;

            // Confidence scales with closed-deal volume (capped)
            double aiConfidence = closedCount == 0
                ? 0
                : Math.Round(Math.Min(98.5, 70 + Math.Log10(closedCount + 1) * 12), 1);

            // Every month on record, then trim to the ones that have actually settled.
            var allMonths = BuildMonthBuckets(deals);
            var observedThrough = deals.Count > 0
                ? deals.Max(d => d.CloseDate ?? d.CreatedDate)
                : DateTime.UtcNow;

            var lastComplete = ReportingPeriod.LastCompleteIndex(
                allMonths.Select(m => (m.Start.AddMonths(1), (double)m.ClosedCount)).ToList(),
                observedThrough);

            var completeMonths = lastComplete >= 0
                ? allMonths.Take(lastComplete + 1).ToList()
                : new List<MonthBucket>();

            var excluded = allMonths.Skip(completeMonths.Count).Select(m => m.Label).ToList();
            if (excluded.Count > 0)
            {
                _logger.LogInformation(
                    "Dashboard KPIs: excluding {Count} still-accumulating month(s) from period comparisons: {Months}. " +
                    "Baselines use complete months through {Through}.",
                    excluded.Count, string.Join(", ", excluded),
                    completeMonths.Count > 0 ? completeMonths[^1].Label : "n/a");
            }

            // Period-over-period changes, computed only between complete months.
            var current = completeMonths.Count >= 1 ? completeMonths[^1] : null;
            var previous = completeMonths.Count >= 2 ? completeMonths[^2] : null;

            decimal? periodRevenueChange = current is not null && previous is not null
                ? ReportingPeriod.PercentChange(previous.WonRevenue, current.WonRevenue)
                : null;
            WarnIfImplausible(periodRevenueChange, "period revenue", previous?.Label, current?.Label);

            double? winRateChange = current is not null && previous is not null
                ? Math.Round(current.WinRate - previous.WinRate, 1)
                : null;

            // Next-month projection from the trailing complete months.
            var recentActuals = completeMonths
                .TakeLast(3)
                .Select(m => m.WonRevenue)
                .Where(v => v > 0)
                .ToList();

            decimal predictedNext = 0;
            if (recentActuals.Count > 0)
            {
                predictedNext = Math.Round(recentActuals.Average() * 1.05m, 0);
            }
            else if (pipelineVal > 0)
            {
                predictedNext = Math.Round(pipelineVal * 0.35m, 0);
            }

            decimal? predictedChange = current is not null
                ? ReportingPeriod.PercentChange(current.WonRevenue, predictedNext)
                : null;
            WarnIfImplausible(predictedChange, "predicted revenue", current?.Label, "forecast");

            var revenueChart = BuildRevenueChart(completeMonths, months, predictedNext);
            var winRateChart = completeMonths
                .TakeLast(months)
                .Select(m => new MonthlyWinRateChartDto { Month = m.Label, WinRate = m.WinRate })
                .ToList();

            // Lag values for the revenue forecast page (last 3 complete months)
            var lags = PadLags(completeMonths.TakeLast(3).Select(m => m.WonRevenue).ToList(), 3);

            return Ok(new
            {
                // Lifetime cumulative. A running total has no meaningful
                // period-over-period % change, so it deliberately carries no badge.
                totalRevenue = FormatMoney(totalRevenueVal),
                totalRevenueRaw = totalRevenueVal,
                totalRevenueBasis = "lifetime_cumulative",
                totalRevenueChange = (string?)null,

                // The period equivalent, for a card that wants a real % badge.
                periodRevenue = FormatMoney(current?.WonRevenue ?? 0),
                periodRevenueRaw = current?.WonRevenue ?? 0,
                periodRevenueChange = FormatPercent(periodRevenueChange),
                periodLabel = current?.Label,
                previousPeriodLabel = previous?.Label,

                predictedRevenue = FormatMoney(predictedNext),
                predictedRevenueRaw = predictedNext,
                predictedChange = FormatPercent(predictedChange),

                winRate = $"{winRate}%",
                winRateRaw = winRate,
                periodWinRate = current is not null ? $"{current.WinRate}%" : null,
                winRateChange = winRateChange.HasValue
                    ? $"{(winRateChange >= 0 ? "+" : "")}{winRateChange}pp"
                    : null,

                aiConfidence = $"{aiConfidence}%",
                aiConfidenceRaw = aiConfidence,
                totalPipelineValue = pipelineVal,
                totalDeals = deals.Count,
                wonDeals = wonCount,

                // Transparency about which months were trusted.
                completeThrough = current?.Label,
                excludedIncompleteMonths = excluded,

                months,
                lag1 = lags.Count > 2 ? lags[2] : 0m,
                lag2 = lags.Count > 1 ? lags[1] : 0m,
                lag3 = lags.Count > 0 ? lags[0] : 0m,
                rollingMean = lags.Count > 0 ? Math.Round(lags.Average(), 0) : 0m,
                revenueChart,
                winRateChart
            });
        }

        private void WarnIfImplausible(decimal? change, string label, string? from, string? to)
        {
            if (!ReportingPeriod.IsImplausible(change)) return;

            _logger.LogWarning(
                "Implausible {Label} change of {Change}% between '{From}' and '{To}'. " +
                "A swing beyond ±{Bound}% usually means the baseline period is incomplete, " +
                "not that the business actually moved that much.",
                label, change, from ?? "n/a", to ?? "n/a", ReportingPeriod.ImplausibleChangePercent);
        }

        /// <summary>
        /// One continuous revenue series: actual values up to the boundary, forecast
        /// values after it. The boundary month carries both, so the two rendered
        /// segments meet at a shared point instead of leaving a gap.
        /// </summary>
        private static List<MonthlyRevenueChartDto> BuildRevenueChart(
            List<MonthBucket> completeMonths, int months, decimal predictedNext)
        {
            var window = completeMonths.TakeLast(months).ToList();
            var chart = window
                .Select(m => new MonthlyRevenueChartDto
                {
                    Month = m.Label,
                    ActualRevenue = m.WonRevenue,
                    PredictedRevenue = null,
                    IsForecast = false
                })
                .ToList();

            if (chart.Count == 0 || predictedNext <= 0) return chart;

            // Boundary point: last actual value doubles as the forecast's origin.
            chart[^1].PredictedRevenue = chart[^1].ActualRevenue;

            var lastMonth = window[^1].Start;
            var value = predictedNext;
            for (var i = 1; i <= 3; i++)
            {
                chart.Add(new MonthlyRevenueChartDto
                {
                    Month = lastMonth.AddMonths(i).ToString("MMM yyyy"),
                    ActualRevenue = null,
                    PredictedRevenue = Math.Round(value, 0),
                    IsForecast = true
                });
                value *= 1.02m;
            }

            return chart;
        }

        private static List<MonthBucket> BuildMonthBuckets(List<Models.Deal> deals)
        {
            if (deals.Count == 0) return new List<MonthBucket>();

            static DateTime Effective(Models.Deal d) => d.CloseDate ?? d.CreatedDate;

            var first = deals.Min(Effective);
            var last = deals.Max(Effective);
            var start = new DateTime(first.Year, first.Month, 1);
            var end = new DateTime(last.Year, last.Month, 1);

            var won = deals.Where(d => d.Status == "Won")
                .GroupBy(d => (Effective(d).Year, Effective(d).Month))
                .ToDictionary(g => g.Key, g => (Revenue: g.Sum(d => d.Value), Count: g.Count()));

            var closed = deals.Where(d => d.Status is "Won" or "Lost")
                .GroupBy(d => (Effective(d).Year, Effective(d).Month))
                .ToDictionary(g => g.Key, g => g.Count());

            var buckets = new List<MonthBucket>();
            for (var m = start; m <= end; m = m.AddMonths(1))
            {
                var key = (m.Year, m.Month);
                won.TryGetValue(key, out var w);
                closed.TryGetValue(key, out var c);
                buckets.Add(new MonthBucket(m, w.Revenue, c, w.Count));
            }

            return buckets;
        }

        private static string? FormatPercent(decimal? value) =>
            value.HasValue ? $"{(value >= 0 ? "+" : "")}{value}%" : null;

        private static List<decimal> PadLags(List<decimal> values, int count)
        {
            var list = values.ToList();
            while (list.Count < count)
            {
                list.Insert(0, list.Count > 0 ? list[0] * 0.95m : 0);
            }
            return list.TakeLast(count).ToList();
        }

        private static string FormatMoney(decimal value)
        {
            if (value >= 1_000_000m) return $"${value / 1_000_000m:F2}M";
            if (value >= 1_000m) return $"${value / 1_000m:F0}K";
            return $"${value:F0}";
        }
    }
}
