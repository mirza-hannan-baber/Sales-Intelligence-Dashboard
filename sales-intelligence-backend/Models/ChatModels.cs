using System.Text.Json.Serialization;

namespace SalesIntelligence.Api.Models
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatHistoryMessage> History { get; set; } = new();
    }

    public class ChatResponse
    {
        public string Answer { get; set; } = string.Empty;
        public string Intent { get; set; } = "general";
        public object? Data { get; set; }
        public List<string> SuggestedQuestions { get; set; } = new();
        public string? Sql { get; set; }
    }

    public class AiIntent
    {
        [JsonPropertyName("intent")]
        public string Intent { get; set; } = "general";

        [JsonPropertyName("operation")]
        public string? Operation { get; set; }

        [JsonPropertyName("targetColumn")]
        public string? TargetColumn { get; set; }

        [JsonPropertyName("filterColumn")]
        public string? FilterColumn { get; set; }

        [JsonPropertyName("filterValue")]
        public string? FilterValue { get; set; }

        [JsonPropertyName("groupBy")]
        public string? GroupBy { get; set; }

        [JsonPropertyName("dateColumn")]
        public string? DateColumn { get; set; }

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("month")]
        public int? Month { get; set; }

        [JsonPropertyName("compareColumns")]
        public List<string>? CompareColumns { get; set; }

        [JsonPropertyName("forecastType")]
        public string? ForecastType { get; set; }

        [JsonPropertyName("requiredAnalysis")]
        public List<string>? RequiredAnalysis { get; set; }
    }

    public class CorrelationFactor
    {
        public string Factor { get; set; } = string.Empty;
        public double Correlation { get; set; }
    }

    public class BusinessRecommendationInsights
    {
        public string HighestWinRateIndustry { get; set; } = "Technology";
        public double HighestWinRate { get; set; } = 72;
        public string LowestWinRateIndustry { get; set; } = "Healthcare";
        public double LowestWinRate { get; set; } = 45;
        public string LongestSalesCycleIndustry { get; set; } = "Healthcare";
        public double AverageSalesCycle { get; set; } = 60;
        public string StrongestFactor { get; set; } = "Sales Cycle Length";
        public double Correlation { get; set; } = -0.62;
    }

    public class ForecastItem
    {
        public string Month { get; set; } = string.Empty;
        public decimal PredictedRevenue { get; set; }
    }

    public class ForecastResult
    {
        public List<ForecastItem> Forecast { get; set; } = new();
        public decimal NextQuarterTotal { get; set; }
        public string Source { get; set; } = "random_forest_revenue_model";
    }
}
