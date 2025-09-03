using System;

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

        public long? WireSizeBytes { get; set; }   // 線路位元組（含壓縮）
        public string? SavedPath { get; set; }
        public long? SavedSizeBytes { get; set; }  // 實際落地大小（解壓後）

        public string? DetectedFormat { get; set; }
        public string? Converter { get; set; }

        public bool Ok { get; set; }
        public string? Error { get; set; }
    }
}
