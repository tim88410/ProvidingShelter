namespace ProvidingShelter.Domain.DTOs
{
    public sealed class PieRequestDto
    {
        public FiltersDto Filters { get; set; } = new();
        public DimensionKey Dimension { get; set; } = DimensionKey.industry;
        public MetricDto Metric { get; set; } = new();
        public LimitDto? Limit { get; set; }
    }

    public sealed class PieItemDto
    {
        public string Label { get; set; } = default!;
        public double Value { get; set; }
        public double? Percentage { get; set; }
    }

    public sealed class PieResultDto
    {
        public object Meta { get; set; } = default!;
        public List<PieItemDto> Items { get; set; } = new();
        public string Unit { get; set; } = "人";
    }
}
