namespace ProvidingShelter.Domain.DTOs
{
    public sealed class HeatmapRequestDto
    {
        public FiltersDto Filters { get; set; } = new();
        public PivotDto Pivot { get; set; } = default!;
        public MetricDto Metric { get; set; } = new();
        public LimitDto? Limit { get; set; }
    }

    public sealed class HeatmapResultDto
    {
        public object Meta { get; set; } = default!;
        public List<string> Rows { get; set; } = new();
        public List<string> Cols { get; set; } = new();
        public List<List<double>> Matrix { get; set; } = new();
        public string Unit { get; set; } = "人";
    }
}
