using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public interface ISchemaDiscoveryService
    {
        Task<DatasetSchema> GetSchemaAsync(CancellationToken cancellationToken = default);
        Task WarmCacheAsync(CancellationToken cancellationToken = default);
        ColumnMetadata? ResolveColumn(string semanticTag, string? preferredTable = null);
        string GetPromptText();
    }
}
