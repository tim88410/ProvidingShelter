namespace ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate
{
    public sealed class SexualAssaultImport
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string SourceFileName { get; private set; } = default!;
        public string StoredFullPath { get; private set; } = default!;
        public string FileHashSha256 { get; private set; } = default!;
        public string CrossTableTitle { get; private set; } = default!; // 檔名/標題備查
        public DateTime ImportedAtUtc { get; private set; } = DateTime.UtcNow;

        public int? PeriodYearStart { get; private set; }
        public int? PeriodYearEnd { get; private set; }

        public CrossCategoryType CategoryType { get; private set; }

        public int RawRowCount { get; private set; }
        public int ParsedRowCount { get; private set; }

        public ICollection<SexualAssaultStat> Stats { get; private set; } = new List<SexualAssaultStat>();

        // 工廠 / 行為（可依需要擴充）
        public SexualAssaultImport(
            string sourceFileName,
            string storedFullPath,
            string fileHashSha256,
            string crossTableTitle,
            CrossCategoryType categoryType,
            int? periodYearStart = null,
            int? periodYearEnd = null)
        {
            SourceFileName = sourceFileName;
            StoredFullPath = storedFullPath;
            FileHashSha256 = fileHashSha256;
            CrossTableTitle = crossTableTitle;
            CategoryType = categoryType;
            PeriodYearStart = periodYearStart;
            PeriodYearEnd = periodYearEnd;
        }

        public void SetCounters(int raw, int parsed)
        {
            RawRowCount = raw;
            ParsedRowCount = parsed;
        }
    }
}
