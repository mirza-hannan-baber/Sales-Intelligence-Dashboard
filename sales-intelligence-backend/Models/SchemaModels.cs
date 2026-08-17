namespace SalesIntelligence.Api.Models
{
    public class ColumnMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Table { get; set; }
        public string DataType { get; set; } = "text";
        public List<string> SampleValues { get; set; } = new();
        public List<string> SemanticTags { get; set; } = new();
        public string? BusinessMeaning { get; set; }
        public string? CsvColumnName { get; set; }
    }

    public class TableMetadata
    {
        public string Name { get; set; } = string.Empty;
        public long RowCount { get; set; }
        public List<ColumnMetadata> Columns { get; set; } = new();
    }

    public class CsvDatasetMetadata
    {
        public string FilePath { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public List<ColumnMetadata> Columns { get; set; } = new();
    }

    public class DatasetSchema
    {
        public CsvDatasetMetadata? Csv { get; set; }
        public List<TableMetadata> Tables { get; set; } = new();
        public Dictionary<string, List<ColumnMetadata>> SemanticIndex { get; set; } = new();
        public string PromptText { get; set; } = string.Empty;
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    }
}
