using System;

namespace ProvidingShelter.Infrastructure.Persistence.Models
{
    public class DatasetResource
    {
        public string DatasetId { get; set; } = default!;
        public string ResourceKey { get; set; } = default!; // 建議 {DatasetId}_{序號}

        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public string? FieldDesc { get; set; }
        public string? FieldDescEn { get; set; }

        public string? Format { get; set; }          // 官方 format
        public string? MediaType { get; set; }       // MIME
        public string? AccessURL { get; set; }
        public string? DownloadURL { get; set; }
        public bool IsApiLike { get; set; }

        public DateTime? LastModified { get; set; }
        public string? ETag { get; set; }
        public string? Checksum { get; set; }

        public long? LastKnownWireSizeBytes { get; set; }
        public long? LastKnownSavedSizeBytes { get; set; }

        public byte Status { get; set; } // 0=unknown,1=ok,2=skipped,3=error
        public DateTime UpdatedAtUtc { get; set; }
    }
}
