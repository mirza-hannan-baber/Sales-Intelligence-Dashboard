namespace SalesIntelligence.Api.Models
{
    public class ChatHistoryMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class SqlAgentPlan
    {
        public bool NeedsSql { get; set; } = true;
        public string IntentType { get; set; } = "factual";
        public string? Sql { get; set; }
        public string? DirectAnswer { get; set; }
        public string? Reasoning { get; set; }
    }

    public class SqlQueryResult
    {
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public int RowCount => Rows.Count;
    }

    public class SqlAgentResponse
    {
        public string Answer { get; set; } = string.Empty;
        public string? Sql { get; set; }
        public SqlQueryResult? QueryResult { get; set; }
        public string Intent { get; set; } = "sql_agent";
    }
}
