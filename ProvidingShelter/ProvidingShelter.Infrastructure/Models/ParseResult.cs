using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;

namespace ProvidingShelter.Infrastructure.Models
{
    public sealed record ParsedStatItem(
        int Year,
        string CityName,
        NationalityCode Nationality,
        CrossCategoryType CategoryType,
        string CategoryKey,
        string CategoryNameZh,
        int Count,
        bool IsTotalRow
    );

    public sealed class ParseResult
    {
        public string CrossTableTitle { get; init; } = string.Empty;
        public int? PeriodYearStart { get; init; }
        public int? PeriodYearEnd { get; init; }
        public int RawRowCount { get; init; }
        public int ParsedRowCount { get; init; }
        public IReadOnlyList<ParsedStatItem> Items { get; init; } = Array.Empty<ParsedStatItem>();
    }
}
