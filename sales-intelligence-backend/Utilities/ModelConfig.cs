using System.Collections.Concurrent;

namespace SalesIntelligence.Api.Utilities
{
    public static class ModelConfig
    {
        private const string ConfigFileName = "model-config.env";

        private static readonly Lazy<Dictionary<string, string>> _values = new(Load);
        private static string _rootDir = Directory.GetCurrentDirectory();

        private static Dictionary<string, string> Load()
        {
            var configPath = FindConfigFile();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (configPath == null)
            {
                return result;
            }

            _rootDir = Path.GetDirectoryName(configPath)!;

            foreach (var raw in File.ReadAllLines(configPath))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || !line.Contains('='))
                {
                    continue;
                }

                var idx = line.IndexOf('=');
                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim().Trim('"').Trim('\'');
                result[key] = value;
            }

            return result;
        }

        private static string? FindConfigFile()
        {
            var searchRoots = new[]
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            foreach (var startDir in searchRoots)
            {
                var current = new DirectoryInfo(startDir);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, ConfigFileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                    current = current.Parent;
                }
            }

            return null;
        }

        /// <summary>Absolute path to the folder containing model-config.env (workspace root).</summary>
        public static string RootDir
        {
            get
            {
                _ = _values.Value; // ensure Load() ran and _rootDir is set
                return _rootDir;
            }
        }

        public static string Get(string key, string defaultValue) =>
            _values.Value.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : defaultValue;

        /// <summary>Absolute path to the dataset CSV used for seeding the database.</summary>
        public static string DatasetPath =>
            Path.GetFullPath(Path.Combine(RootDir, Get("DATASET_PATH", "dataset/clean_crm_data.csv")));

        /// <summary>Absolute path to the directory holding the model artifacts.</summary>
        public static string ModelDir =>
            Path.GetFullPath(Path.Combine(RootDir, Get("MODEL_DIR", "dataset")));
    }
}
