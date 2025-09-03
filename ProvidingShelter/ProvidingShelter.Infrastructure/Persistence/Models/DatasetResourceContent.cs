using System;

namespace ProvidingShelter.Infrastructure.Persistence.Models
{
    public class DatasetResourceContent
    {
        public string DatasetId { get; set; } = default!;
        public string ResourceKey { get; set; } = default!;

        public string StorageMode { get; set; } = "File"; // File | DB
        public string? ContentPath { get; set; }          // File 模式
        public string? ContentJson { get; set; }          // DB 模式 / 小檔預覽

        public long? JsonSizeBytes { get; set; }
        public long? WireSizeBytes { get; set; }
        public long? SavedSizeBytes { get; set; }

        public string? ContentHash { get; set; }
        public DateTime ConvertedAtUtc { get; set; }
    }
}
