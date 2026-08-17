using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IDataAnalysisService
    {
        Task<List<string>> GetAgentNamesAsync();
        Task<List<string>> GetSectorsAsync();
        Task<object> ExecuteAnalysisAsync(AiIntent intent);
        Task<List<CorrelationFactor>> CalculateCorrelationAsync();
        Task<BusinessRecommendationInsights> GetRecommendationInsightsAsync();
    }
}
