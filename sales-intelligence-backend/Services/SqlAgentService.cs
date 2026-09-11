using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public class SqlAgentService : ISqlAgentService
    {
        private readonly IGroqAgentService _groqAgent;
        private readonly ISqlQueryExecutor _executor;
        private readonly ISchemaDiscoveryService _schema;
        private readonly ILogger<SqlAgentService> _logger;

        public SqlAgentService(
            IGroqAgentService groqAgent,
            ISqlQueryExecutor executor,
            ISchemaDiscoveryService schema,
            ILogger<SqlAgentService> logger)
        {
            _groqAgent = groqAgent;
            _executor = executor;
            _schema = schema;
            _logger = logger;
        }

        public async Task<SqlAgentResponse> AnswerAsync(
            string question,
            IReadOnlyList<ChatHistoryMessage> history,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var groqResp = await _groqAgent.AskAsync(question, cancellationToken: cancellationToken);
                return new SqlAgentResponse
                {
                    Answer = groqResp.Answer,
                    Sql = groqResp.SqlUsed,
                    Intent = "sql_agent"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL Agent question: {Question}", question);
                return new SqlAgentResponse
                {
                    Answer = "I couldn't analyze that question right now. Please try rephrasing — for example: \"Which industry has the highest win rate?\" or \"Top 5 employees by revenue in 2025\".",
                    Intent = "sql_agent_error"
                };
            }
        }
    }
}
