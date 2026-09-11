using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IGroqAgentService
    {
        Task<AgentAskResponse> AskAsync(string question, int? datasetId = null, CancellationToken cancellationToken = default);
    }
}
