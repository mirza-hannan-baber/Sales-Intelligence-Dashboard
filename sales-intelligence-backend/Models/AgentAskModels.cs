namespace SalesIntelligence.Api.Models
{
    public class AgentAskRequest
    {
        public string Question { get; set; } = string.Empty;
        public int? DatasetId { get; set; }
    }

    public class AgentAskResponse
    {
        public string Answer { get; set; } = string.Empty;
        public string SqlUsed { get; set; } = string.Empty;
    }
}
