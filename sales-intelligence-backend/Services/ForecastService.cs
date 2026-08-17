using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SalesIntelligence.Api.Data;
using SalesIntelligence.Api.DTOs;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public class ForecastService : IForecastService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPredictionService _predictionService;

        public ForecastService(ApplicationDbContext db, IPredictionService predictionService)
        {
            _db = db;
            _predictionService = predictionService;
        }

        public async Task<ForecastResult> GetNextQuarterForecastAsync()
        {
            var wonDeals = await _db.Deals.AsNoTracking().Where(d => d.Status == "Won").ToListAsync();

            var anchorDate = wonDeals.Count > 0
                ? wonDeals.Max(d => d.CloseDate ?? d.CreatedDate)
                : new DateTime(2026, 3, 31);

            var nextMonth1 = new DateTime(anchorDate.Year, anchorDate.Month, 1).AddMonths(1);

            // Build a 12-month history so we can compute the lag-revenue model's 9 features.
            var series = BuildMonthlyRevenue(wonDeals, anchorDate, 12);
            while (series.Count < 12)
            {
                series.Insert(0, series.Count > 0 ? series[0] * 0.95m : 100000m);
            }

            var forecastItems = new List<ForecastItem>();
            bool usedModel = true;

            for (int i = 0; i < 3; i++)
            {
                var monthDate = nextMonth1.AddMonths(i);

                // Lags are measured back from the most recent value in the series.
                decimal lag1 = series[^1];
                decimal lag2 = series[^2];
                decimal lag3 = series[^3];
                decimal lag6 = series[^6];
                decimal lag12 = series[^12];

                var last3 = new[] { lag1, lag2, lag3 };
                var rollingMean3 = Math.Round(last3.Average(), 0);
                var rollingStd3 = SampleStd(last3);

                var predicted = await PredictMonthAsync(new CompanyRevenuePredictRequest
                {
                    Lag1 = (float)lag1,
                    Lag2 = (float)lag2,
                    Lag3 = (float)lag3,
                    RollingMean = (float)rollingMean3,
                    Lag6 = (float)lag6,
                    Lag12 = (float)lag12,
                    RollingMean3 = (float)rollingMean3,
                    RollingStd3 = (float)rollingStd3,
                    Month = monthDate.Month,
                    QuarterNumber = (monthDate.Month - 1) / 3 + 1
                });

                if (predicted == null)
                {
                    usedModel = false;
                    predicted = Math.Round(rollingMean3 * (1.02m + i * 0.03m), 0);
                }

                forecastItems.Add(new ForecastItem
                {
                    Month = monthDate.ToString("MMMM yyyy"),
                    PredictedRevenue = predicted.Value
                });

                // Roll the window forward with the freshly predicted month.
                series.Add(predicted.Value);
            }

            return new ForecastResult
            {
                Forecast = forecastItems,
                NextQuarterTotal = forecastItems.Sum(f => f.PredictedRevenue),
                Source = usedModel ? "lag_revenue_model" : "historical_average_fallback"
            };
        }

        private static decimal SampleStd(IReadOnlyList<decimal> values)
        {
            if (values.Count < 2) return 0m;
            var mean = values.Average();
            var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
            return Math.Round((decimal)Math.Sqrt((double)variance), 0);
        }

        private async Task<decimal?> PredictMonthAsync(CompanyRevenuePredictRequest request)
        {
            try
            {
                var result = await _predictionService.PredictCompanyRevenueAsync(request, "chat-forecast");

                if (result is JsonElement el)
                {
                    if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("error", out _))
                    {
                        return null;
                    }

                    if (el.TryGetProperty("predicted_revenue", out var pred) && pred.TryGetDecimal(out var value))
                    {
                        return Math.Round(value, 0);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static List<decimal> BuildMonthlyRevenue(List<Deal> wonDeals, DateTime anchorDate, int months)
        {
            var end = new DateTime(anchorDate.Year, anchorDate.Month, 1);
            var start = end.AddMonths(-(months - 1));
            var result = new List<decimal>();

            for (int i = 0; i < months; i++)
            {
                var month = start.AddMonths(i);
                var sum = wonDeals
                    .Where(d =>
                    {
                        var date = d.CloseDate ?? d.CreatedDate;
                        return date.Year == month.Year && date.Month == month.Month;
                    })
                    .Sum(d => d.Value);
                result.Add(sum);
            }

            return result;
        }
    }
}
