using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface ISqlAgentService
    {
        Task<SqlAgentResponse> AnswerAsync(
            string question,
            IReadOnlyList<ChatHistoryMessage> history,
            CancellationToken cancellationToken = default);
    }
}
