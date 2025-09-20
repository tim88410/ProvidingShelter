namespace ProvidingShelter.Infrastructure.Persistence.Models
{
    public class DatasetResourceFetch
    {
        public long Id { get; set; }
        public string DatasetId { get; set; } = default!;
        public string ResourceKey { get; set; } = default!;

        public DateTime FetchAtUtc { get; set; }

        public int? HttpStatus { get; set; }
        public string? ContentType { get; set; }
        public string? Encoding { get; set; }
        public string? ETag { get; set; }
        public DateTime? LastModified { get; set; }

        public long? WireSizeBytes { get; set; }
        public string? SavedPath { get; set; }
        public long? SavedSizeBytes { get; set; }

        public string? DetectedFormat { get; set; }
        public string? Converter { get; set; }

        public bool Ok { get; set; }
        public string? Error { get; set; }
    }
}
