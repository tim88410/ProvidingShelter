namespace ProvidingShelter.Domain.DTOs
{
    public sealed class ChoroplethRequestDto
    {
        public FiltersDto Filters { get; set; } = new();
        public MetricDto Metric { get; set; } = new();
    }

    public sealed class ChoroplethFeatureDto
    {
        public string City { get; set; } = default!;
        public double Value { get; set; }
        public double? Percentage { get; set; }
        public string? Adcode { get; set; }
    }

    public sealed class ScatterRequestDto
    {
        public FiltersDto Filters { get; set; } = new();
        public string XIndustry { get; set; } = "製造業";
        public string YIndustry { get; set; } = "營造業";
    }

    public sealed class ScatterPointDto
    {
        public string City { get; set; } = default!;
        public double X { get; set; }
        public double Y { get; set; }
        public double? Size { get; set; }
    }

    public sealed class ScatterResultDto
    {
        public List<ScatterPointDto> Points { get; set; } = new();
        public string Unit { get; set; } = "人";
    }

    public sealed class HierarchyRequestDto
    {
        public FiltersDto Filters { get; set; } = new();
        public List<DimensionKey> Hierarchy { get; set; } = new() { DimensionKey.nationality, DimensionKey.industry };
        public Dictionary<string, int>? Top /* e.g. { "nationality": 6 } */ { get; set; }
    }

    public sealed class HierarchyNodeDto
    {
        public string Name { get; set; } = default!;
        public double Value { get; set; }
        public List<HierarchyNodeDto>? Children { get; set; }
    }

    public sealed class HierarchyResultDto
    {
        public List<HierarchyNodeDto> Nodes { get; set; } = new();
        public string Unit { get; set; } = "人";
    }
}
