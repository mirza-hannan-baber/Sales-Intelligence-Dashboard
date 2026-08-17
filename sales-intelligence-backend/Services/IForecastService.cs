using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IForecastService
    {
        Task<ForecastResult> GetNextQuarterForecastAsync();
    }
}
