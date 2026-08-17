using System.Globalization;
using System.Text;

namespace SalesIntelligence.Api.Utilities
{
    public static class CsvHelper
    {

        public static string? FindCsvPath()
        {
            var configured = ModelConfig.DatasetPath;
            return File.Exists(configured) ? configured : null;
        }

        public static Dictionary<string, int> BuildHeaderIndex(string headerLine)
        {
            var headers = SplitCsvLine(headerLine);
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                var name = headers[i].Trim();
                if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                {
                    map[name] = i;
                }
            }
            return map;
        }

        public static string GetField(string[] parts, Dictionary<string, int> headerIndex, string column)
        {
            return headerIndex.TryGetValue(column, out var idx) && idx < parts.Length
                ? parts[idx]
                : string.Empty;
        }

        public static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
        }

        public static string InferType(IEnumerable<string> sampleValues)
        {
            var values = sampleValues.Where(v => !string.IsNullOrWhiteSpace(v)).Take(20).ToList();
            if (values.Count == 0) return "text";

            if (values.All(v => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
                return "integer";

            if (values.All(v => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _)))
                return "decimal";

            if (values.All(v => DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                return "datetime";

            if (values.All(v => v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                 v.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                                 v == "0" || v == "1"))
                return "boolean";

            return "text";
        }
    }
}
