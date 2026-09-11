using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SalesIntelligence.Api.Models;

namespace SalesIntelligence.Api.Services
{
    public class GroqAgentService : IGroqAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly ISqlQueryExecutor _executor;
        private readonly ISchemaDiscoveryService _schemaService;
        private readonly IConfiguration _config;
        private readonly ILogger<GroqAgentService> _logger;

        private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private const string PrimaryModel = "openai/gpt-oss-120b";
        private const string FallbackModel = "openai/gpt-oss-20b";

        public GroqAgentService(
            HttpClient httpClient,
            ISqlQueryExecutor executor,
            ISchemaDiscoveryService schemaService,
            IConfiguration config,
            ILogger<GroqAgentService> logger)
        {
            _httpClient = httpClient;
            _executor = executor;
            _schemaService = schemaService;
            _config = config;
            _logger = logger;
        }

        public async Task<AgentAskResponse> AskAsync(string question, int? datasetId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new AgentAskResponse
                {
                    Answer = "Please provide a question about your sales data.",
                    SqlUsed = string.Empty
                };
            }

            var primaryKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? _config["GROQ_API_KEY"] ?? string.Empty;
            var backupKey = Environment.GetEnvironmentVariable("GROQ_API_KEY_BACKUP") ?? _config["GROQ_API_KEY_BACKUP"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(primaryKey) && string.IsNullOrWhiteSpace(backupKey))
            {
                _logger.LogWarning("GROQ_API_KEY is not set in environment or .env file.");
                return new AgentAskResponse
                {
                    Answer = "GROQ_API_KEY is missing or empty in .env. Please set GROQ_API_KEY in the backend .env file to enable AI answers.",
                    SqlUsed = string.Empty
                };
            }

            var lowerQ = question.ToLowerInvariant().Trim();
            if (IsGreeting(lowerQ))
            {
                return new AgentAskResponse
                {
                    Answer = "Hello! I'm your Sales Intelligence AI Assistant. Ask me anything about your sales data — revenue, top performing sales agents, lost deals, industry win rates, forecasts, or how sales metrics are calculated.",
                    SqlUsed = string.Empty
                };
            }

            if (IsAdvisoryQuestion(lowerQ))
            {
                return await HandleAdvisoryPathAsync(question, datasetId, primaryKey, backupKey, cancellationToken);
            }
            else
            {
                return await HandleDirectSqlPathAsync(question, datasetId, primaryKey, backupKey, cancellationToken);
            }
        }

        private static bool IsGreeting(string q)
        {
            var trimmed = q.TrimEnd('.', '!', '?');
            return trimmed is "hi" or "hye" or "hello" or "hey" or "greetings" or "good morning" or "good afternoon" or "good evening" or "help" or "who are you" or "what can you do";
        }

        private static bool IsAdvisoryQuestion(string lowerQuestion)
        {
            return lowerQuestion.Contains("how do we") ||
                   lowerQuestion.Contains("how can we") ||
                   lowerQuestion.Contains("suggest") ||
                   lowerQuestion.Contains("recommend") ||
                   lowerQuestion.Contains("improve") ||
                   lowerQuestion.Contains("strategy") ||
                   lowerQuestion.Contains("factor") ||
                   lowerQuestion.Contains("influence") ||
                   lowerQuestion.Contains("driver") ||
                   lowerQuestion.Contains("correlat") ||
                   lowerQuestion.Contains("why");
        }

        private async Task<AgentAskResponse> HandleAdvisoryPathAsync(
            string question,
            int? datasetId,
            string primaryKey,
            string backupKey,
            CancellationToken cancellationToken)
        {
            string dsWhere = (datasetId.HasValue && datasetId.Value > 0) ? $" AND DatasetId = {datasetId.Value}" : "";
            string dsWhereOnly = (datasetId.HasValue && datasetId.Value > 0) ? $" WHERE DatasetId = {datasetId.Value}" : "";

            // Multi-analysis aggregate queries across CRM dimensions
            var queries = new (string Name, string Sql)[]
            {
                ("Win Rate by Sector",
                 $"SELECT Sector, COUNT(*) as TotalDeals, SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) as WonDeals, ROUND(100.0 * SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN Status IN ('Won', 'Lost') THEN 1 ELSE 0 END), 0), 1) as WinRatePercent FROM Deals WHERE Status IN ('Won', 'Lost'){dsWhere} GROUP BY Sector ORDER BY WinRatePercent DESC"),

                ("Win Rate by Region",
                 $"SELECT Region, COUNT(*) as TotalDeals, SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) as WonDeals, ROUND(100.0 * SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN Status IN ('Won', 'Lost') THEN 1 ELSE 0 END), 0), 1) as WinRatePercent FROM Deals WHERE Status IN ('Won', 'Lost') AND Region IS NOT NULL{dsWhere} GROUP BY Region ORDER BY WinRatePercent DESC"),

                ("Win Rate by Product",
                 $"SELECT Product, COUNT(*) as TotalDeals, SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) as WonDeals, ROUND(100.0 * SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN Status IN ('Won', 'Lost') THEN 1 ELSE 0 END), 0), 1) as WinRatePercent FROM Deals WHERE Status IN ('Won', 'Lost'){dsWhere} GROUP BY Product ORDER BY WinRatePercent DESC"),

                ("Win Rate by Cycle-Length Bucket",
                 $"SELECT CASE WHEN DealDurationDays <= 30 THEN '0-30 days' WHEN DealDurationDays <= 60 THEN '31-60 days' WHEN DealDurationDays <= 90 THEN '61-90 days' ELSE '90+ days' END as CycleBucket, COUNT(*) as TotalDeals, SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) as WonDeals, ROUND(100.0 * SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN Status IN ('Won', 'Lost') THEN 1 ELSE 0 END), 0), 1) as WinRatePercent FROM Deals WHERE Status IN ('Won', 'Lost'){dsWhere} GROUP BY CycleBucket ORDER BY WinRatePercent DESC"),

                ("Top Reps by Revenue",
                 $"SELECT Owner, SUM(Value) as TotalRevenue, COUNT(*) as WonDeals FROM Deals WHERE Status = 'Won'{dsWhere} GROUP BY Owner ORDER BY TotalRevenue DESC LIMIT 5"),

                ("Monthly Revenue Trend (Last 12 Months)",
                 $"SELECT strftime('%Y-%m', COALESCE(CloseDate, CreatedDate)) as Month, SUM(Value) as MonthlyRevenue FROM Deals WHERE Status = 'Won'{dsWhere} GROUP BY Month ORDER BY Month DESC LIMIT 12"),

                ("Average Deal Size by Sector",
                 $"SELECT Sector, ROUND(AVG(Value), 2) as AvgDealSize FROM Deals WHERE Status = 'Won'{dsWhere} GROUP BY Sector ORDER BY AvgDealSize DESC")
            };

            var aggregateResults = new StringBuilder();
            var sqlList = new List<string>();

            foreach (var (name, sql) in queries)
            {
                try
                {
                    sqlList.Add(sql);
                    var result = await _executor.ExecuteAsync(sql, cancellationToken);
                    aggregateResults.AppendLine($"=== {name} ===");
                    aggregateResults.AppendLine(JsonSerializer.Serialize(result.Rows));
                    aggregateResults.AppendLine();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed executing aggregate query {Name}", name);
                }
            }

            var systemPrompt =
                "You are an expert Sales Intelligence Advisor.\n" +
                "The user is asking an open-ended strategic or analytical question about sales drivers, win rates, or performance.\n" +
                "You have been provided real aggregate query results from the live CRM database below.\n" +
                "CRITICAL INSTRUCTION: Use ONLY the numbers in these query results to reason and give your answer. " +
                "Do NOT invent any fake numbers or statistics. Highlight key factors (e.g. sectors/products/regions/cycle lengths with high vs low win rates). If data is missing, state so directly.";

            var userPrompt =
                $"User Question: {question}\n\n" +
                $"Real Database Aggregate Results:\n{aggregateResults}";

            var messages = new List<GroqMessage>
            {
                new("system", systemPrompt),
                new("user", userPrompt)
            };

            var answer = await CallGroqApiWithFallbackAsync(messages, primaryKey, backupKey, cancellationToken);
            return new AgentAskResponse
            {
                Answer = answer,
                SqlUsed = string.Join(";\n\n", sqlList)
            };
        }

        private async Task<AgentAskResponse> HandleDirectSqlPathAsync(
            string question,
            int? datasetId,
            string primaryKey,
            string backupKey,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("==================================================");
            _logger.LogInformation("[AGENT DEBUG] (a) Exact Question Received: \"{Question}\"", question);

            var schemaText = _schemaService.GetPromptText();
            if (string.IsNullOrWhiteSpace(schemaText))
            {
                await _schemaService.GetSchemaAsync(cancellationToken);
                schemaText = _schemaService.GetPromptText();
            }

            string datasetMandate = (datasetId.HasValue && datasetId.Value > 0)
                ? $"\n7. DATASET FILTER (CRITICAL MANDATE): The user is viewing dataset ID = {datasetId.Value}. You MUST include `DatasetId = {datasetId.Value}` in the WHERE clause for Deals, Agents, or Accounts."
                : "";

            var systemPrompt =
                "You are a database SQL expert for a SQLite CRM database.\n" +
                $"{schemaText}\n\n" +
                "Your only job is to write ONE SQLite SELECT query that answers the user's question.\n" +
                "RULES:\n" +
                "1. Output ONLY the raw SQL query. Do not wrap in markdown code fences. Do not include any explanations.\n" +
                "2. The query MUST be a SELECT statement. Write operations (DROP, DELETE, UPDATE, INSERT, ALTER, ATTACH, PRAGMA) are strictly forbidden.\n" +
                "3. Reference real table names: Deals, Agents, or Accounts.\n" +
                "4. Make sure column names match the schema exactly (e.g. Value, Owner, Sector, Status, CreatedDate, CloseDate).\n" +
                "5. NUMERIC CASTING MANDATE: In SQLite, TotalRevenue, Value, ProposedValue, Revenue, AccountRevenue are TEXT. You MUST use CAST(col AS REAL) when doing numeric filtering (e.g. CAST(TotalRevenue AS REAL) > 1000000), aggregations (SUM(CAST(Value AS REAL))), or sorting.\n" +
                "6. FLOAT DIVISION MANDATE (CRITICAL): In SQLite, dividing integer counts returns 0 (e.g. 15 / 100 = 0). When calculating win rates, percentages, or ratios, ALWAYS multiply by 100.0 first or cast to REAL: e.g. ROUND(100.0 * SUM(CASE WHEN Status = 'Won' THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN Status IN ('Won', 'Lost') THEN 1 ELSE 0 END), 0), 1). NEVER write `SUM(...) / COUNT(*) * 100` because integer division will evaluate to 0 across all rows!" +
                datasetMandate;

            var messages = new List<GroqMessage>
            {
                new("system", systemPrompt),
                new("user", question)
            };

            string validatedSql = string.Empty;
            SqlQueryResult? queryResult = null;
            string? lastError = null;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0 && !string.IsNullOrWhiteSpace(lastError))
                {
                    messages.Add(new("user", $"The previous SQL failed validation or execution with error: {lastError}. Please write a corrected ONE SQLite SELECT query matching the schema. Output ONLY the SQL."));
                }

                var rawResponse = await CallGroqApiWithFallbackAsync(messages, primaryKey, backupKey, cancellationToken);
                var cleanedSql = CleanSqlString(rawResponse);

                _logger.LogInformation("[AGENT DEBUG] Attempt {Attempt}: Raw SQL Generated by Groq: {RawSql}", attempt + 1, rawResponse);

                var (isValid, normSql, err) = SqlQueryValidator.ValidateAndNormalize(cleanedSql);
                if (!isValid)
                {
                    lastError = err;
                    _logger.LogWarning("[AGENT DEBUG] SQL Validation failed on attempt {Attempt}: {Error}. Raw: {Raw}", attempt + 1, err, rawResponse);
                    messages.Add(new("assistant", cleanedSql));
                    continue;
                }

                try
                {
                    queryResult = await _executor.ExecuteAsync(normSql, cancellationToken);
                    validatedSql = normSql;
                    _logger.LogInformation("[AGENT DEBUG] (b) Exact SQL Executed: {Sql}", validatedSql);
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning(ex, "[AGENT DEBUG] SQL Execution failed on attempt {Attempt}: {Error}", attempt + 1, ex.Message);
                    messages.Add(new("assistant", normSql));
                }
            }

            if (queryResult == null || string.IsNullOrWhiteSpace(validatedSql))
            {
                _logger.LogError("[AGENT DEBUG] Failed to produce valid SQL for question: {Question}", question);
                return new AgentAskResponse
                {
                    Answer = "I'm sorry, I was unable to generate a valid database query for that question. Please try rephrasing your question.",
                    SqlUsed = string.Empty
                };
            }

            // Step 6: Narrate real result in plain English
            var resultJson = JsonSerializer.Serialize(queryResult.Rows);
            _logger.LogInformation("[AGENT DEBUG] (c) Exact Raw Result ({RowCount} rows): {ResultJson}", queryResult.RowCount, resultJson);

            var narrationSystemPrompt =
                "You are an AI Sales Assistant explaining CRM database query results to a user in plain English.\n" +
                "RULES:\n" +
                "1. Direct Answer: Answer the user's specific question clearly and concisely in plain English.\n" +
                "2. Conceptual Questions: If the user asks which field a metric depends on (e.g., 'Win rates depend on which field?'), state clearly that Win Rate depends on the 'Status' field (calculating the percentage of Won deals out of total closed Won and Lost deals).\n" +
                "3. Accuracy: Rely strictly on the query result numbers for stats — never invent numbers or print raw JSON/unformatted lists of row data.\n" +
                "4. Concise Tone: Give a focused, direct answer to the user's question. Do not append canned or repetitive closing offers.";

            var narrationUserPrompt =
                $"Question: {question}\n" +
                $"SQL Executed: {validatedSql}\n" +
                $"Real Query Result ({queryResult.RowCount} rows):\n{resultJson}";

            var narrationMessages = new List<GroqMessage>
            {
                new("system", narrationSystemPrompt),
                new("user", narrationUserPrompt)
            };

            var narrationAnswer = await CallGroqApiWithFallbackAsync(narrationMessages, primaryKey, backupKey, cancellationToken);
            _logger.LogInformation("[AGENT DEBUG] Final Narration Answer: \"{Answer}\"", narrationAnswer);
            _logger.LogInformation("==================================================");

            return new AgentAskResponse
            {
                Answer = narrationAnswer,
                SqlUsed = validatedSql
            };
        }

        private static string CleanSqlString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var sql = raw.Trim();

            // Extract SQL block if wrapped in ```sql ... ``` anywhere in text
            var fenceMatch = System.Text.RegularExpressions.Regex.Match(
                sql,
                @"```(?:sql)?\s*([\s\S]*?)```",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (fenceMatch.Success)
            {
                sql = fenceMatch.Groups[1].Value.Trim();
            }

            // Fallback: search for SELECT or WITH keyword
            var startIdx = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (startIdx < 0) startIdx = sql.IndexOf("WITH", StringComparison.OrdinalIgnoreCase);
            if (startIdx >= 0)
            {
                sql = sql.Substring(startIdx);
                var endFence = sql.IndexOf("```");
                if (endFence >= 0) sql = sql.Substring(0, endFence);
            }

            return sql.Trim();
        }

        private async Task<string> CallGroqApiWithFallbackAsync(
            List<GroqMessage> messages,
            string primaryKey,
            string backupKey,
            CancellationToken cancellationToken)
        {
            try
            {
                return await CallGroqSingleKeyAsync(messages, primaryKey, PrimaryModel, cancellationToken);
            }
            catch (Exception ex) when (IsRateLimitOrQuotaException(ex))
            {
                if (!string.IsNullOrWhiteSpace(backupKey))
                {
                    _logger.LogWarning(ex, "Primary Groq API key failed with rate-limit / quota error. Retrying request with GROQ_API_KEY_BACKUP...");
                    try
                    {
                        return await CallGroqSingleKeyAsync(messages, backupKey, PrimaryModel, cancellationToken);
                    }
                    catch
                    {
                        // Fall back to secondary model below
                    }
                }

                _logger.LogWarning("Groq API rate limit hit (429) on {PrimaryModel}. Retrying with fallback model {FallbackModel}...", PrimaryModel, FallbackModel);
                try
                {
                    return await CallGroqSingleKeyAsync(messages, primaryKey, FallbackModel, cancellationToken);
                }
                catch
                {
                    _logger.LogWarning("Fallback model failed. Waiting 2.5s before final retry...");
                    await Task.Delay(2500, cancellationToken);
                    return await CallGroqSingleKeyAsync(messages, primaryKey, FallbackModel, cancellationToken);
                }
            }
        }

        private async Task<string> CallGroqSingleKeyAsync(
            List<GroqMessage> messages,
            string apiKey,
            string modelName,
            CancellationToken cancellationToken)
        {
            var reqObj = new GroqRequest
            {
                Model = modelName,
                Temperature = 0,
                Messages = messages
            };

            var jsonBody = JsonSerializer.Serialize(reqObj);
            using var req = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            var respBody = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 429 || respBody.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || respBody.Contains("quota", StringComparison.OrdinalIgnoreCase))
                {
                    throw new GroqRateLimitException($"Groq API HTTP {(int)resp.StatusCode}: {respBody}");
                }

                // If model deprecated or not found, try fallback model
                if (respBody.Contains("model_not_found", StringComparison.OrdinalIgnoreCase) || respBody.Contains("decommissioned", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Groq model {Model} failed, trying fallback {FallbackModel}", modelName, FallbackModel);
                    reqObj.Model = FallbackModel;
                    using var retryReq = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint);
                    retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                    retryReq.Content = new StringContent(JsonSerializer.Serialize(reqObj), Encoding.UTF8, "application/json");
                    using var retryResp = await _httpClient.SendAsync(retryReq, cancellationToken);
                    var retryBody = await retryResp.Content.ReadAsStringAsync(cancellationToken);

                    if (retryResp.IsSuccessStatusCode)
                    {
                        var parsedRetry = JsonSerializer.Deserialize<GroqResponse>(retryBody);
                        return parsedRetry?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
                    }
                }

                throw new HttpRequestException($"Groq API HTTP {(int)resp.StatusCode}: {respBody}");
            }

            var parsed = JsonSerializer.Deserialize<GroqResponse>(respBody);
            var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
            return content;
        }

        private static bool IsRateLimitOrQuotaException(Exception ex)
        {
            if (ex is GroqRateLimitException) return true;
            var msg = ex.Message.ToLowerInvariant();
            return msg.Contains("429") || msg.Contains("rate limit") || msg.Contains("quota");
        }

        private sealed class GroqRateLimitException : Exception
        {
            public GroqRateLimitException(string message) : base(message) { }
        }

        private sealed class GroqRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; } = 0;

            [JsonPropertyName("messages")]
            public List<GroqMessage> Messages { get; set; } = new();
        }

        private sealed class GroqMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            public GroqMessage() { }
            public GroqMessage(string role, string content)
            {
                Role = role;
                Content = content;
            }
        }

        private sealed class GroqResponse
        {
            [JsonPropertyName("choices")]
            public List<GroqChoice>? Choices { get; set; }
        }

        private sealed class GroqChoice
        {
            [JsonPropertyName("message")]
            public GroqMessage? Message { get; set; }
        }
    }
}
