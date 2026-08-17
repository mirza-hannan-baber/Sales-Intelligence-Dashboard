using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IGroqAgentService
    {
        Task<AgentAskResponse> AskAsync(string question, CancellationToken cancellationToken = default);
    }
}
