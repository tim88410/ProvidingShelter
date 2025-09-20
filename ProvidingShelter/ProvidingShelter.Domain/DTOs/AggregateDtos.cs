namespace ProvidingShelter.Domain.DTOs
{
    // ========= Aggregate 請求 & 回應 =========

    /// <summary>
    /// 通用彙整查詢的請求物件（Series/Bar/Line/Area 共用）
    /// </summary>
    /// <remarks>
    /// 適用端點：
    /// - <b>/api/aggregate</b>（萬用）  
    /// - <b>/api/charts/line</b>、<b>/api/charts/bar</b>、<b>/api/charts/area</b>（實際上都是餵進同一種結構）  
    /// - 若將 <see cref="Output"/> 設為 <c>"heatmap"</c>，並提供 <see cref="ViewDto.Pivot"/>，也可產出熱力圖資料結構。
    ///   但專用熱力端點通常使用獨立的 HeatmapRequestDto。
    /// </remarks>
    public sealed class AggregateRequestDto
    {
        /// <summary>篩選條件（年分、縣市、國籍、行業…）</summary>
        public FiltersDto Filters { get; set; } = new();

        /// <summary>視圖（X 軸 / 系列分組 / 堆疊 / 交叉表）</summary>
        public ViewDto View { get; set; } = new();

        /// <summary>指標設定（count / 正規化 / 移動平均）</summary>
        public MetricDto Metric { get; set; } = new();

        /// <summary>
        /// 限制與排序（TopN、是否合併其他…）
        /// </summary>
        /// <remarks>常見用法：壓縮圖例數量、Pie 的 Top N。</remarks>
        public LimitDto? Limit { get; set; }

        /// <summary>
        /// 輸出資料型態：
        /// <list type="bullet">
        /// <item><term>series</term><description>分類序列（給 Line/Bar/Area）</description></item>
        /// <item><term>table</term><description>表格（可選，若有支援）</description></item>
        /// <item><term>heatmap</term><description>熱力矩陣（需搭配 View.Pivot）</description></item>
        /// </list>
        /// </summary>
        public string Output { get; set; } = "series";
    }

    /// <summary>
    /// 單一資料列（圖例）— 一條線或一組長條
    /// </summary>
    /// <remarks>對應 ECharts 的一個 series。</remarks>
    public sealed class SeriesLineDto
    {
        /// <summary>系列名稱（圖例標籤）</summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// 序列資料（與回傳的 Categories 逐一對齊）
        /// </summary>
        public List<double> Data { get; set; } = new();

        /// <summary>
        /// 是否為「其他」彙整桶（Limit.TopN &amp; OtherBucket 生效時會為 true）
        /// </summary>
        public bool? IsOther { get; set; }
    }

    /// <summary>
    /// 系列型圖表（Line/Bar/Area）的回傳結構
    /// </summary>
    /// <remarks>
    /// 適用端點：/api/aggregate（Output=series）、/api/charts/line、/api/charts/bar、/api/charts/area
    /// </remarks>
    public sealed class SeriesResultDto
    {
        /// <summary>
        /// 中繼資料（會回傳 x/series/stack、filtersApplied 等）
        /// </summary>
        public object Meta { get; set; } = default!;

        /// <summary>
        /// X 軸分類（字串陣列；與各 <see cref="SeriesLineDto.Data"/> 對齊）
        /// </summary>
        public List<string> Categories { get; set; } = new();

        /// <summary>
        /// 多條序列（每條對應圖例的一條線/一組長條）
        /// </summary>
        public List<SeriesLineDto> Series { get; set; } = new();

        /// <summary>
        /// （可選）每個類別的總和（例如堆疊總計或總量輔助線）
        /// </summary>
        public List<double>? Totals { get; set; }

        /// <summary>
        /// 單位：<c>"人"</c> 或 <c>"%"</c>（當 Normalize=percent 時）
        /// </summary>
        public string Unit { get; set; } = "人";
    }
}
