using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.Utilities;

namespace SalesIntelligence.Api.Services
{
    /// <summary>Yearly model inputs, in the order the model was trained on:
    /// revenue, years_active, deals_worked, win_rate.</summary>
    public record EmployeeYearlyFeatures(
        string SalesAgent,
        int Year,
        double Revenue,
        double YearsActive,
        double DealsWorked,
        double WinRate);

    /// <summary>Quarterly model inputs, in training order:
    /// deals_worked, win_rate, avg_deal_size, avg_cycle_days, revenue.</summary>
    public record EmployeeQuarterlyFeatures(
        string SalesAgent,
        string Quarter,
        double DealsWorked,
        double WinRate,
        double AvgDealSize,
        double AvgCycleDays,
        double Revenue);

    public interface IEmployeeFeatureService
    {
        Task<EmployeeYearlyFeatures?> GetYearlyFeaturesAsync(string salesAgent);
        Task<EmployeeQuarterlyFeatures?> GetQuarterlyFeaturesAsync(string salesAgent);
        Task<Dictionary<string, double>> GetWinRatePercentByAgentAsync();
    }

    /// <summary>
    /// Rebuilds the feature vectors the employee models were trained on, straight
    /// from the CRM tables.
    ///
    /// The training notebooks (see dataset/Train_on_the_new_clean_dataset.ipynb)
    /// defined the features as follows, and these definitions are reproduced here
    /// exactly — they were verified to match rep_year_panel.csv (175/175 rows) and
    /// rep_quarter_panel.csv (560/560 rows) to the last decimal:
    ///
    ///   revenue        sum of the per-deal account `revenue` column over WON deals,
    ///                  grouped by CLOSE period (not close_value, which is ~1000x smaller)
    ///   deals_worked   count of ALL deals, grouped by ENGAGE period
    ///   win_rate       mean of is_won over ALL deals in the engage period,
    ///                  as a FRACTION in [0,1] — never a 0-100 percentage
    ///   years_active   period year minus the rep's first engage year
    ///   avg_deal_size  mean deal_value_proposed over all deals engaged in the quarter
    ///   avg_cycle_days mean cycle length over CLOSED deals only (open deals were NaN
    ///                  in training and skipped by pandas)
    ///
    /// Features are always built for the latest COMPLETE period. The trailing period
    /// in this CRM (2026) has no engaged deals at all and only a partial tail of
    /// closes, so feeding it to the model pushes every rep far outside the training
    /// distribution, where the trees collapse onto one leaf and return an identical
    /// prediction for everybody.
    /// </summary>
    public class EmployeeFeatureService : IEmployeeFeatureService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<EmployeeFeatureService> _logger;

        public EmployeeFeatureService(ApplicationDbContext db, ILogger<EmployeeFeatureService> logger)
        {
            _db = db;
            _logger = logger;
        }

        private sealed record DealRow(
            string Owner,
            string Status,
            DateTime EngageDate,
            DateTime? CloseDate,
            decimal AccountRevenue,
            decimal ProposedValue,
            int DurationDays);

        private List<DealRow>? _deals;

        private async Task<List<DealRow>> LoadAsync()
        {
            return _deals ??= await _db.Deals
                .AsNoTracking()
                .Select(d => new DealRow(
                    d.Owner, d.Status, d.CreatedDate, d.CloseDate,
                    d.AccountRevenue, d.ProposedValue, d.DealDurationDays))
                .ToListAsync();
        }

        private static DateTime ObservedThrough(List<DealRow> deals) =>
            deals.Count == 0
                ? DateTime.UtcNow
                : deals.Max(d => d.CloseDate ?? d.EngageDate);

        /// <summary>Latest calendar year that is fully elapsed and no longer accumulating.</summary>
        private static int? LatestCompleteYear(List<DealRow> deals)
        {
            if (deals.Count == 0) return null;

            var years = deals.Select(d => d.EngageDate.Year)
                .Concat(deals.Where(d => d.CloseDate.HasValue).Select(d => d.CloseDate!.Value.Year))
                .Distinct().OrderBy(y => y).ToList();

            var periods = years
                .Select(y => (EndExclusive: new DateTime(y + 1, 1, 1),
                              Volume: (double)deals.Count(d => d.EngageDate.Year == y)))
                .ToList();

            var idx = ReportingPeriod.LastCompleteIndex(periods, ObservedThrough(deals));
            return idx < 0 ? null : years[idx];
        }

        /// <summary>Latest calendar quarter that is fully elapsed and no longer accumulating.</summary>
        private static (int Year, int Quarter)? LatestCompleteQuarter(List<DealRow> deals)
        {
            if (deals.Count == 0) return null;

            static (int Year, int Quarter) QuarterOf(DateTime d) => (d.Year, (d.Month - 1) / 3 + 1);

            var quarters = deals.Select(d => QuarterOf(d.EngageDate))
                .Concat(deals.Where(d => d.CloseDate.HasValue).Select(d => QuarterOf(d.CloseDate!.Value)))
                .Distinct().OrderBy(q => q.Year).ThenBy(q => q.Quarter).ToList();

            var periods = quarters
                .Select(q => (EndExclusive: new DateTime(q.Year, (q.Quarter - 1) * 3 + 1, 1).AddMonths(3),
                              Volume: (double)deals.Count(d => QuarterOf(d.EngageDate) == q)))
                .ToList();

            var idx = ReportingPeriod.LastCompleteIndex(periods, ObservedThrough(deals));
            return idx < 0 ? null : quarters[idx];
        }

        public async Task<EmployeeYearlyFeatures?> GetYearlyFeaturesAsync(string salesAgent)
        {
            if (string.IsNullOrWhiteSpace(salesAgent)) return null;

            var deals = await LoadAsync();
            var year = LatestCompleteYear(deals);
            if (year is null) return null;

            var mine = deals.Where(d => d.Owner == salesAgent).ToList();
            if (mine.Count == 0)
            {
                _logger.LogWarning("No deals found for sales agent '{SalesAgent}'.", salesAgent);
                return null;
            }

            // revenue: account revenue on WON deals that CLOSED in the target year.
            var revenue = mine
                .Where(d => d.Status == "Won" && d.CloseDate.HasValue && d.CloseDate.Value.Year == year)
                .Sum(d => (double)d.AccountRevenue);

            // deals_worked / win_rate: all deals ENGAGED in the target year.
            var engaged = mine.Where(d => d.EngageDate.Year == year).ToList();
            var dealsWorked = engaged.Count;
            var winRate = dealsWorked > 0
                ? engaged.Count(d => d.Status == "Won") / (double)dealsWorked
                : 0.0;

            var yearsActive = Math.Max(0, year.Value - mine.Min(d => d.EngageDate.Year));

            return new EmployeeYearlyFeatures(
                salesAgent, year.Value, revenue, yearsActive, dealsWorked, winRate);
        }

        public async Task<EmployeeQuarterlyFeatures?> GetQuarterlyFeaturesAsync(string salesAgent)
        {
            if (string.IsNullOrWhiteSpace(salesAgent)) return null;

            var deals = await LoadAsync();
            var quarter = LatestCompleteQuarter(deals);
            if (quarter is null) return null;

            var (year, q) = quarter.Value;
            var mine = deals.Where(d => d.Owner == salesAgent).ToList();
            if (mine.Count == 0)
            {
                _logger.LogWarning("No deals found for sales agent '{SalesAgent}'.", salesAgent);
                return null;
            }

            bool InQuarter(DateTime d) => d.Year == year && (d.Month - 1) / 3 + 1 == q;

            var engaged = mine.Where(d => InQuarter(d.EngageDate)).ToList();
            var dealsWorked = engaged.Count;
            var winRate = dealsWorked > 0
                ? engaged.Count(d => d.Status == "Won") / (double)dealsWorked
                : 0.0;
            var avgDealSize = dealsWorked > 0
                ? engaged.Average(d => (double)d.ProposedValue)
                : 0.0;

            // Open deals had a null cycle length during training and were skipped.
            var closedInQuarter = engaged.Where(d => d.CloseDate.HasValue).ToList();
            var avgCycleDays = closedInQuarter.Count > 0
                ? closedInQuarter.Average(d => (double)d.DurationDays)
                : 0.0;

            var revenue = mine
                .Where(d => d.Status == "Won" && d.CloseDate.HasValue && InQuarter(d.CloseDate.Value))
                .Sum(d => (double)d.AccountRevenue);

            return new EmployeeQuarterlyFeatures(
                salesAgent, $"{year}Q{q}", dealsWorked, winRate, avgDealSize, avgCycleDays, revenue);
        }

        /// <summary>Real win rate per rep, as a 0-100 percentage, over every deal on record.</summary>
        public async Task<Dictionary<string, double>> GetWinRatePercentByAgentAsync()
        {
            var deals = await LoadAsync();
            return deals
                .Where(d => !string.IsNullOrWhiteSpace(d.Owner))
                .GroupBy(d => d.Owner)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Count(d => d.Status == "Won") * 100.0 / g.Count(), 1));
        }
    }
}
