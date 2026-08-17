using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface IChatService
    {
        Task<ChatResponse> ProcessMessageAsync(ChatRequest request);
    }
}
