namespace ProvidingShelter.Domain.DTOs
{
    public sealed class PanelRequestDto
    {
        public FiltersDto Filters { get; set; } = new();

        /// <summary>目前僅支援 "city"</summary>
        public string PanelBy { get; set; } = "city";

        /// <summary>X 建議 year、Series 建議 industry；stack 交給前端視覺</summary>
        public ViewDto View { get; set; } = new()
        {
            X = DimensionKey.year,
            Series = DimensionKey.industry,
            Stack = false
        };

        public MetricDto Metric { get; set; } = new();

        /// <summary>每個面板的 Series（行業）TopN；其他合併為「其他」</summary>
        public LimitDto? SeriesLimit { get; set; }

        /// <summary>是否補 0，確保各面板年份完整對齊</summary>
        public bool IncludeZeros { get; set; } = true;
    }

    public sealed class PanelDto
    {
        public string Key { get; set; } = default!;        // e.g. 臺北市
        public string Title { get; set; } = default!;      // e.g. 臺北市 2007–2024 各行業
        public List<string> Categories { get; set; } = new(); // 年份字串
        public List<SeriesLineDto> Series { get; set; } = new();
    }

    public sealed class PanelResultDto
    {
        public object Meta { get; set; } = default!;
        public List<PanelDto> Panels { get; set; } = new();
    }
}
