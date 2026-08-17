using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.DTOs;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IPredictionService
    {
        Task<object> PredictCompanyRevenueAsync(CompanyRevenuePredictRequest req, string username);
        Task<object> PredictWinRateAsync(WinRatePredictRequest req, string username);
        Task<object> PredictEmployeePerformanceAsync(EmployeePerformancePredictRequest req, string username);
        Task<object> PredictEmployeeRevenueAsync(EmployeeRevenuePredictRequest req, string username);
        Task<List<PredictionLog>> GetHistoryAsync();
    }

    public class PredictionService : IPredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _db;
        private readonly IEmployeeFeatureService _features;
        private readonly ILogger<PredictionService> _logger;
        private readonly string _mlServiceBaseUrl;

        public PredictionService(
            HttpClient httpClient,
            IConfiguration config,
            ApplicationDbContext db,
            IEmployeeFeatureService features,
            ILogger<PredictionService> logger)
        {
            _httpClient = httpClient;
            _db = db;
            _features = features;
            _logger = logger;
            _mlServiceBaseUrl = config["MlService:BaseUrl"] ?? "http://localhost:5001";
        }

        public async Task<object> PredictCompanyRevenueAsync(CompanyRevenuePredictRequest req, string username)
        {
            // Send the lag-revenue-model true features. Null values are derived by the
            // ML service from lag_1/2/3 + rolling_mean, keeping the old 4-field contract working.
            var payload = new
            {
                lag_1 = req.Lag1,
                lag_2 = req.Lag2,
                lag_3 = req.Lag3,
                rolling_mean = req.RollingMean,
                lag_6 = req.Lag6,
                lag_12 = req.Lag12,
                rolling_mean_3 = req.RollingMean3,
                rolling_std_3 = req.RollingStd3,
                month = req.Month,
                quarter = req.QuarterNumber
            };
            return await CallMlServiceAsync("/predict/company-revenue", payload, "Company Revenue Forecast", username);
        }

        public async Task<object> PredictWinRateAsync(WinRatePredictRequest req, string username)
        {
            // Send the win-rate classifier's true numerical + categorical features.
            var payload = new
            {
                deal_value_proposed = req.DealValueProposed,
                employees = req.Employees,
                sales_cycle_days = req.SalesCycleDays,
                engage_month = req.EngageMonth,
                engage_quarter = req.EngageQuarter,
                sector = req.Sector,
                product = req.Product,
                regional_office = req.RegionalOffice,
                office_location = req.OfficeLocation,
                sales_agent = req.SalesAgent,
                month = req.Month,
                quarter = req.Quarter,
                is_quarter_end = req.IsQuarterEnd
            };
            return await CallMlServiceAsync("/predict/win-rate", payload, "Win Rate Forecast", username);
        }

        public async Task<object> PredictEmployeePerformanceAsync(EmployeePerformancePredictRequest req, string username)
        {
            // The database is the source of truth for this rep's features. Anything the
            // browser sent is only a fallback, so a stale or partial client payload can
            // never make two different reps score identically.
            var f = await _features.GetQuarterlyFeaturesAsync(req.SalesAgent);

            var dealsWorked = f?.DealsWorked ?? (double?)req.DealsWorked ?? req.TotalDealsLag1;
            var winRate = f?.WinRate ?? ToFraction(req.WinRate ?? req.WinRateLag1);
            var avgDealSize = f?.AvgDealSize ?? (double?)req.AvgDealSize ?? req.AverageDealValueLag1;
            var avgCycleDays = f?.AvgCycleDays ?? (double?)req.AvgCycleDays ?? req.AverageSalesCycleLag1;
            var revenue = f?.Revenue ?? (double?)req.Revenue ?? req.TotalRevenueLag1;

            _logger.LogInformation(
                "Employee performance prediction | sales_agent='{SalesAgent}' period={Period} source={Source} " +
                "features=[deals_worked={DealsWorked}, win_rate={WinRate}, avg_deal_size={AvgDealSize}, " +
                "avg_cycle_days={AvgCycleDays}, revenue={Revenue}]",
                req.SalesAgent, f?.Quarter ?? "n/a", f is null ? "request" : "database",
                dealsWorked, winRate, avgDealSize, avgCycleDays, revenue);

            if (f is null)
            {
                _logger.LogWarning(
                    "No database features for '{SalesAgent}'; falling back to the request payload. " +
                    "Predictions may not reflect this rep.", req.SalesAgent);
            }

            var payload = new
            {
                sales_agent = req.SalesAgent,
                deals_worked = dealsWorked,
                win_rate = winRate,
                avg_deal_size = avgDealSize,
                avg_cycle_days = avgCycleDays,
                revenue,
                feature_period = f?.Quarter,
                feature_source = f is null ? "request" : "database"
            };
            return await CallMlServiceAsync("/predict/employee-performance", payload, $"Employee Performance: {req.SalesAgent}", username);
        }

        public async Task<object> PredictEmployeeRevenueAsync(EmployeeRevenuePredictRequest req, string username)
        {
            // Same contract as above: look the rep up by name and rebuild the exact
            // four features the yearly model was trained on, in training order.
            var f = await _features.GetYearlyFeaturesAsync(req.SalesAgent);

            var revenue = f?.Revenue ?? (double?)req.Revenue ?? req.RollingMean12 * 12f;
            var yearsActive = f?.YearsActive ?? (double?)req.YearsActive ?? 2d;
            var dealsWorked = f?.DealsWorked ?? (double?)req.DealsWorked ?? 0d;
            var winRate = f?.WinRate ?? ToFraction(req.WinRate) ?? 0d;

            _logger.LogInformation(
                "Employee revenue prediction | sales_agent='{SalesAgent}' period={Period} source={Source} " +
                "features=[revenue={Revenue}, years_active={YearsActive}, deals_worked={DealsWorked}, win_rate={WinRate}]",
                req.SalesAgent, f?.Year.ToString() ?? "n/a", f is null ? "request" : "database",
                revenue, yearsActive, dealsWorked, winRate);

            if (f is null)
            {
                _logger.LogWarning(
                    "No database features for '{SalesAgent}'; falling back to the request payload. " +
                    "Predictions may not reflect this rep.", req.SalesAgent);
            }

            var payload = new
            {
                sales_agent = req.SalesAgent,
                revenue,
                years_active = yearsActive,
                deals_worked = dealsWorked,
                win_rate = winRate,
                feature_period = f?.Year,
                feature_source = f is null ? "request" : "database"
            };
            return await CallMlServiceAsync("/predict/employee-revenue", payload, $"Employee Revenue: {req.SalesAgent}", username);
        }

        /// <summary>
        /// Both employee models were trained on win rate as a fraction in [0,1].
        /// Callers still sending the legacy 0-100 percentage are converted here;
        /// passing 38.0 where the model expects 0.38 puts every rep far outside the
        /// training range, which is what made all predictions come back identical.
        /// </summary>
        private static double? ToFraction(float? winRate)
        {
            if (winRate is null) return null;
            return winRate.Value > 1.0f ? winRate.Value / 100.0 : winRate.Value;
        }

        public async Task<List<PredictionLog>> GetHistoryAsync()
        {
            return await _db.PredictionLogs.OrderByDescending(p => p.Timestamp).Take(50).ToListAsync();
        }

        private async Task<object> CallMlServiceAsync(string path, object payload, string predictionType, string username)
        {
            var url = $"{_mlServiceBaseUrl}{path}";
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            string responseString = "";
            try
            {
                var response = await _httpClient.PostAsync(url, content);
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                responseString = JsonSerializer.Serialize(new
                {
                    error = "ML prediction service is currently unavailable. Please check the ML service.",
                    details = ex.Message,
                    status = 503
                });
            }

            var log = new PredictionLog
            {
                PredictionType = predictionType,
                Target = predictionType,
                RequestedByUsername = username,
                Timestamp = DateTime.UtcNow,
                InputParametersJson = jsonPayload,
                ResultJson = responseString,
                ModelVersion = "1.0.0"
            };

            _db.PredictionLogs.Add(log);
            await _db.SaveChangesAsync();

            try
            {
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.Clone();
            }
            catch (Exception parseEx)
            {
                return new
                {
                    error = "ML prediction service returned an invalid response.",
                    details = parseEx.Message,
                    raw = responseString
                };
            }
        }
    }
}
