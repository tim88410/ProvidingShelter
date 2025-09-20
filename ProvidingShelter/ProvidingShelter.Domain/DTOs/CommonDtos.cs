namespace ProvidingShelter.Domain.DTOs
{
    // ========= 枚舉（給 View/Metric 用） =========

    /// <summary>
    /// 維度鍵（對應前端 X 軸或系列分組）
    /// </summary>
    /// <remarks>
    /// 建議同時支援數字與字串 enum 反序列化：
    /// - 數字：none=0, year=1, city=2, nationality=3, industry=4
    /// </remarks>
    public enum DimensionKey { none = 0, year = 1, city = 2, nationality = 3, industry = 4 }
    /// <summary>
    /// 正規化（標準化）模式
    /// </summary>
    /// <remarks>
    /// none=不正規化（回傳原始件數，單位=「人」）；
    /// percent=回傳比例（0~1，前端可 *100 顯示%，單位=「%」）。
    /// </remarks>
    public enum NormalizeMode { none = 0, percent = 1 }
    // ========= 篩選條件 =========

    /// <summary>
    /// 年份過濾（單一 / 區間 / 多選）
    /// </summary>
    public sealed class SingleOrRangeYear
    {
        /// <summary>模式：single | range | multi</summary>
        public string Mode { get; set; } = "single";

        /// <summary>單一年份（Mode=single 用）</summary>
        public int? Value { get; set; }

        /// <summary>起年（Mode=range 用）</summary>
        public int? From { get; set; }

        /// <summary>迄年（Mode=range 用）</summary>
        public int? To { get; set; }

        /// <summary>多選年份（Mode=multi 用）</summary>
        public List<int>? Values { get; set; }
    }
    /// <summary>
    /// 查詢的篩選條件集合
    /// </summary>
    /// <remarks>
    /// 適用圖表：Line / Bar / Area / Aggregate；Pie / Heatmap / Choropleth / Scatter / Sunburst / Treemap 也會使用到類似的 Filters。
    /// </remarks>
    public sealed class FiltersDto
    {
        /// <summary>年份過濾（單/區間/多選）</summary>
        public SingleOrRangeYear Years { get; set; } = new();

        /// <summary>縣市名稱清單（空或 null=全國彙總）。可多選以彙總多個縣市。</summary>
        public List<string>? Cities { get; set; }

        /// <summary>國籍顯示名稱清單（例如：「外國籍-越南」「外國籍-印尼」）。空或 null=不限。</summary>
        public List<string>? Nationalities { get; set; }

        /// <summary>行業名稱清單（例如：「製造業」「家庭看護」）。空或 null=不限。</summary>
        public List<string>? Industries { get; set; }

        /// <summary>分類型別（保留欄位；目前資料以行業為主）。</summary>
        public string? CategoryType { get; set; }
    }
    // ========= 視圖（X/Series/堆疊…） =========

    /// <summary>
    /// 交叉表指定（主要給 Heatmap 使用）
    /// </summary>
    public sealed class PivotDto
    {
        /// <summary>列維度（DimensionKey）</summary>
        public DimensionKey Rows { get; set; }

        /// <summary>行維度（DimensionKey）</summary>
        public DimensionKey Cols { get; set; }
    }

    /// <summary>
    /// 圖表視圖設定（X 軸 / 系列分組 / 是否堆疊 / 交叉表等）
    /// </summary>
    /// <remarks>
    /// 適用圖表：
    /// - Line/Bar/Area/Aggregate：使用 X / Series / Stack
    /// - Heatmap（若走 Aggregate 並輸出 heatmap 時）：使用 Pivot
    /// - Pie/Scatter/Choropleth/Sunburst/Treemap：不用 View（各自有獨立 DTO）
    /// </remarks>
    public sealed class ViewDto
    {
        /// <summary>X 軸維度（year/city/nationality/industry）</summary>
        public DimensionKey X { get; set; } = DimensionKey.industry;

        /// <summary>系列（圖例）維度（可為 null 或 none 代表單一序列）</summary>
        public DimensionKey? Series { get; set; } = null;

        /// <summary>是否堆疊顯示（Bar/Area 可用；Line 理論可疊但較少用）</summary>
        public bool Stack { get; set; } = false;

        /// <summary>交叉表設定（主要給 heatmap 使用）</summary>
        public PivotDto? Pivot { get; set; }
    }
    // ========= 指標與截斷 =========

    /// <summary>
    /// 指標設定（件數/正規化/移動平均）
    /// </summary>
    /// <remarks>
    /// 適用圖表：Line / Bar / Area / Aggregate（series/heatmap）
    /// </remarks>
    public sealed class MetricDto
    {
        /// <summary>指標型態（目前支援 "count" 件數）</summary>
        public string Type { get; set; } = "count";

        /// <summary>正規化（none=原始件數；percent=比例 0~1）</summary>
        public NormalizeMode Normalize { get; set; } = NormalizeMode.none;

        /// <summary>
        /// 移動平均視窗（0=不平滑；例如 3=3 期拖尾移動平均）。
        /// 只建議在 X=year 時使用。
        /// </summary>
        public int MovingAverage { get; set; } = 0;
    }

    /// <summary>
    /// Top/排序/「其他」彙整等限制條件
    /// </summary>
    /// <remarks>
    /// 適用圖表：Pie（最常用）；Line/Bar/Area 也可用於壓縮圖例數量（TopN）。
    /// </remarks>
    public sealed class LimitDto
    {
        /// <summary>只取前 N 名（依 SortBy/Order 決定排序）</summary>
        public int? TopN { get; set; }

        /// <summary>是否將 TopN 之外彙整成「其他」</summary>
        public bool OtherBucket { get; set; } = false;

        /// <summary>排序欄位：value（數值）或 label（名稱）</summary>
        public string SortBy { get; set; } = "value";

        /// <summary>排序方向：desc | asc</summary>
        public string Order { get; set; } = "desc";
    }


    /// <summary>
    /// 維度選單的中繼資料（用來產生前端下拉式選單）。
    /// 來源資料已去重、排序，且排除了彙總列（IsTotalRow）。
    /// </summary>
    /// <remarks>
    /// 常見用途：前端「年份 / 縣市 / 國籍 / 行業」下拉選單的資料來源。
    /// 典型端點：GET /api/meta/dimensions（或你專案中對應的 meta 查詢端點）。
    /// </remarks>
    public sealed class DimensionsMetaDto
    {
        /// <summary>
        /// 可用年份清單（遞增排序；例如：2007..2024）。
        /// 前端可用來提供：單年、區間或多選的年份選擇。
        /// 對應到 FiltersDto.Years。
        /// </summary>
        public List<int> Years { get; set; } = new();

        /// <summary>
        /// 可用縣市顯示名稱（已正規化，如「臺北市」「新北市」…，以字典序排序）。
        /// 前端可直接綁到 Cities 下拉；對應到 FiltersDto.Cities。
        /// </summary>
        public List<string> Cities { get; set; } = new();

        /// <summary>
        /// 可用國籍顯示名稱（例如：「外國籍-越南」「外國籍-印尼」「外國籍-其他」…）。
        /// 這些顯示字串會在查詢時映射回內部的 NationalityCode 列舉。
        /// 對應到 FiltersDto.Nationalities。
        /// </summary>
        public List<string> Nationalities { get; set; } = new();

        /// <summary>
        /// 可用行業顯示名稱（例如：「製造業」「營造業」「家庭看護」「家庭幫傭」…，以字典序排序）。
        /// 對應到 FiltersDto.Industries。
        /// </summary>
        public List<string> Industries { get; set; } = new();
    }
}
