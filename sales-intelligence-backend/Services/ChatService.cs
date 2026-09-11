using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public class ChatService : IChatService
    {
        private readonly ISqlAgentService _sqlAgent;
        private readonly IGroqAgentService _groqAgentService;
        private readonly IDataAnalysisService _dataAnalysisService;
        private readonly IForecastService _forecastService;
        private readonly IRecommendationAnalysisService _recommendationAnalysis;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ISqlAgentService sqlAgent,
            IGroqAgentService groqAgentService,
            IDataAnalysisService dataAnalysisService,
            IForecastService forecastService,
            IRecommendationAnalysisService recommendationAnalysis,
            ILogger<ChatService> logger)
        {
            _sqlAgent = sqlAgent;
            _groqAgentService = groqAgentService;
            _dataAnalysisService = dataAnalysisService;
            _forecastService = forecastService;
            _recommendationAnalysis = recommendationAnalysis;
            _logger = logger;
        }

        public async Task<ChatResponse> ProcessMessageAsync(ChatRequest request)
        {
            var userMsg = request.Message ?? string.Empty;
            var history = request.History ?? new List<ChatHistoryMessage>();

            if (string.IsNullOrWhiteSpace(userMsg))
            {
                return new ChatResponse
                {
                    Answer = "How can I help you analyze your sales data today?",
                    Intent = "general",
                    SuggestedQuestions = DefaultSuggestions()
                };
            }

            var lower = userMsg.ToLowerInvariant();

            // ML forecast — existing model, not LLM prediction
            if (DataQuestionParser.IsForecastQuestion(lower))
            {
                return await HandleForecastAsync(userMsg);
            }

            // Correlation / factors — existing deterministic analysis
            if (DataQuestionParser.IsCorrelationQuestion(lower))
            {
                try
                {
                    if(DataQuestionParser.IsRecommendationQuestion(lower) || IsStrategicQuestion(lower))
                    {
                        return await HandleRecommendationAsync(userMsg, history);
                    }
                    else{}
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Correlation analysis failed, falling back to SQL agent.");
                }
                return await HandleCorrelationAsync(userMsg, history);
            }

            // Recommendation / strategy questions — multi-analysis then synthesis
            if (DataQuestionParser.IsRecommendationQuestion(lower) || IsStrategicQuestion(lower))
            {
                return await HandleRecommendationAsync(userMsg, history);
            }


            // Primary path: dynamic SQL agent with chat history
            try
            {
                var agentResult = await _sqlAgent.AnswerAsync(userMsg, history);
                if (agentResult.Intent != "sql_agent_error")
                {
                    return BuildResponse(agentResult.Answer, agentResult.Intent, agentResult.QueryResult);
                }

                _logger.LogWarning("SQL agent failed, falling back to rule-based analysis.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQL agent threw; falling back to rule-based analysis.");
            }

            // Fallback when SQL unavailable
            if (DataQuestionParser.IsDataQuestion(userMsg))
            {
                var agents = await _dataAnalysisService.GetAgentNamesAsync();
                var sectors = await _dataAnalysisService.GetSectorsAsync();
                var intentObj = DataQuestionParser.Parse(userMsg, agents, sectors);
                intentObj = IntentRefiner.Refine(userMsg, intentObj);
                return await HandleDataAnalysisAsync(userMsg, intentObj);
            }

            return new ChatResponse
            {
                Answer = "I can help you analyze your sales data. Ask about revenue, deals, employees, industries, win rates, forecasts, or sales strategy.",
                Intent = "off_topic",
                SuggestedQuestions = DefaultSuggestions()
            };
        }

        private async Task<ChatResponse> HandleDataAnalysisAsync(string userMsg, AiIntent intentObj)
        {
            var analysisResult = await _dataAnalysisService.ExecuteAnalysisAsync(intentObj);
            var answer = DataAnswerFormatter.Format(userMsg, analysisResult);

            return BuildResponse(answer, "data_analysis", analysisResult);
        }

        private async Task<ChatResponse> HandleForecastAsync(string userMsg)
        {
            var forecastResult = await _forecastService.GetNextQuarterForecastAsync();
            var answer = DataAnswerFormatter.Format(userMsg, forecastResult);
            return BuildResponse(answer, "forecast", forecastResult);
        }

        private async Task<ChatResponse> HandleCorrelationAsync(string userMsg, IReadOnlyList<ChatHistoryMessage> history)
        {
            var correlationList = await _dataAnalysisService.CalculateCorrelationAsync();
            var answer = DataAnswerFormatter.Format(userMsg, correlationList);

            // Add causation disclaimer
            if (!answer.Contains("associated", StringComparison.OrdinalIgnoreCase))
            {
                answer += " Note: these are statistical associations, not proven causes.";
            }

            return BuildResponse(answer, "correlation_analysis", correlationList);
        }

        private async Task<ChatResponse> HandleRecommendationAsync(string userMsg, IReadOnlyList<ChatHistoryMessage> history)
        {
            var insights = await _recommendationAnalysis.GatherInsightsAsync();
            var fallback = _recommendationAnalysis.FormatDeterministicRecommendations(insights);

            string answer = fallback;
            try
            {
                var groqResponse = await _groqAgentService.AskAsync(userMsg);
                if (!string.IsNullOrWhiteSpace(groqResponse.Answer) && !ContainsRawJson(groqResponse.Answer))
                {
                    answer = groqResponse.Answer;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recommendation synthesis failed, using deterministic fallback");
            }

            return BuildResponse(answer, "business_recommendation", insights);
        }

        private static ChatResponse BuildResponse(string answer, string intent, object? data)
        {
            return new ChatResponse
            {
                Answer = answer,
                Intent = intent,
                Data = intent is "forecast" or "correlation_analysis" or "business_recommendation" or "data_analysis"
                    ? data
                    : null,
                SuggestedQuestions = DefaultSuggestions()
            };
        }

        private static bool IsStrategicQuestion(string lower) =>
            lower.Contains("why are we losing") ||
            lower.Contains("why do we lose") ||
            lower.Contains("what should we focus") ||
            lower.Contains("what should management") ||
            lower.Contains("how to improve") ||
            lower.Contains("increase revenue") ||
            lower.Contains("increase sales") ||
            lower.Contains("boost sales");

        private static bool ContainsRawJson(string text) =>
            text.Contains("{") && text.Contains("}") &&
            (text.Contains("columns") || text.Contains("rows") || text.Contains("\":"));

        private static List<string> DefaultSuggestions() =>
            new()
            {
                "How can we increase our sales?",
                "Which employee generated the highest sales in 2025?",
                "Which industry has the highest win rate?",
                "Compare medical and technology sectors",
                "What is the next quarter revenue forecast?"
            };
    }
}
