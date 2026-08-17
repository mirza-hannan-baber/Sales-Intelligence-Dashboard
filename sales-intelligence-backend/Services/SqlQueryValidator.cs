using System.Text.RegularExpressions;

namespace SalesIntelligence.Api.Services
{
    public static class SqlQueryValidator
    {
        private static readonly Regex BlockedPattern = new(
            @"\b(INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|REPLACE|ATTACH|DETACH|VACUUM|REINDEX|TRUNCATE|GRANT|REVOKE|MERGE|EXEC|EXECUTE)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex PragmaPattern = new(@"\bPRAGMA\b", RegexOptions.IgnoreCase);

        public static (bool IsValid, string NormalizedSql, string? Error) ValidateAndNormalize(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return (false, string.Empty, "SQL query is empty.");
            }

            var normalized = sql.Trim();
            normalized = Regex.Replace(normalized, @"^```(?:sql)?\s*", string.Empty, RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"\s*```$", string.Empty);

            // Single statement only
            var statements = normalized
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statements.Length == 0)
            {
                return (false, string.Empty, "SQL query is empty.");
            }
            if (statements.Length > 1)
            {
                return (false, string.Empty, "Only one SQL statement is allowed.");
            }

            normalized = statements[0];

            if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                !normalized.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                return (false, string.Empty, "Only SELECT queries (or WITH ... SELECT) are allowed.");
            }

            if (BlockedPattern.IsMatch(normalized))
            {
                return (false, string.Empty, "Write operations are not allowed.");
            }

            if (PragmaPattern.IsMatch(normalized))
            {
                return (false, string.Empty, "PRAGMA statements are not allowed.");
            }

            if (!Regex.IsMatch(normalized, @"\bLIMIT\s+\d+", RegexOptions.IgnoreCase))
            {
                normalized += " LIMIT 200";
            }

            return (true, normalized, null);
        }
    }
}
