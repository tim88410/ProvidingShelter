using ClosedXML.Excel;
using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;
using ProvidingShelter.Infrastructure.Models;
using System.Text.RegularExpressions;

namespace ProvidingShelter.Infrastructure.Service.ExternalService
{
    public interface ISexualAssaultNationalityStatisticsParser
    {
        Task<ParseResult> ParseAsync(string xlsxFullPath, CancellationToken ct = default);
    }

    /// <summary>
    /// 解析對象 性侵害案件通報外籍被害人國籍別與行業別交叉統計(統計期間為2024年)
    /// 解析「性侵害通報：國籍別 × 行業別交叉統計」(XLSX/ODS 轉出後的 XLSX)。
    /// 假設表頭包含：年份｜縣市｜國籍別；第二層標籤為「行業別」，其右側依序為各行業欄，最後可能有「總計/合計」欄。
    /// </summary>
    public sealed class ClosedXmlSexualAssaultNationalityStatisticsParser : ISexualAssaultNationalityStatisticsParser
    {
        public Task<ParseResult> ParseAsync(string xlsxFullPath, CancellationToken ct = default)
        {
            using var wb = new XLWorkbook(xlsxFullPath);
            var ws = wb.Worksheet(1);

            string V(IXLCell cell)
            {
                var rng = cell.MergedRange();
                var top = rng != null ? rng.FirstCell() : cell;
                var s = top.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(s) ? string.Empty : s!;
            }

            // --- 掃前 30x30 找表頭關鍵字 ---
            int headerRow1 = -1, headerRow2 = -1;
            int colYear = -1, colCity = -1, colNation = -1, colCategoryLabel = -1; // 「行業別」所在欄
            var lastRow = ws.LastRowUsed().RowNumber();
            var lastCol = ws.LastColumnUsed().ColumnNumber();

            for (int r = 1; r <= Math.Min(30, lastRow); r++)
            {
                for (int c = 1; c <= Math.Min(30, lastCol); c++)
                {
                    var t = V(ws.Cell(r, c));
                    if (t == "年份") { headerRow1 = r; colYear = c; }
                    if (t == "縣市") { headerRow1 = r; colCity = c; }
                    if (t == "國籍別") { headerRow1 = r; colNation = c; }
                    if (t == "行業別") { headerRow2 = r; colCategoryLabel = c; }
                }
            }
            if (headerRow1 < 0 || headerRow2 < 0 || colYear < 0 || colCity < 0 || colNation < 0 || colCategoryLabel < 0)
                throw new InvalidOperationException("找不到表頭（年份/縣市/國籍別/行業別）。");

            // --- 收集行業類別欄（從「行業別」右一欄開始，直到遇到 總計/合計 或空白停止）---
            var categoryCols = new List<(int Col, string Zh)>();
            int? totalCol = null;

            for (int c = colCategoryLabel + 1; c <= lastCol; c++)
            {
                var name = V(ws.Cell(headerRow2, c));
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (name is "總計" or "合計")
                {
                    totalCol = c;
                    break;
                }
                categoryCols.Add((c, name));
            }
            if (!totalCol.HasValue)
            {
                for (int c = colCategoryLabel + 1; c <= lastCol; c++)
                {
                    var name = V(ws.Cell(headerRow2, c));
                    if (name is "總計" or "合計") { totalCol = c; break; }
                }
            }

            // --- 擷取「統計期間為YYYY年至YYYY年」 ---
            int? periodStart = null, periodEnd = null;
            var rxYear = new Regex(@"(\d{4})");
            for (int r = 1; r <= Math.Min(5, lastRow); r++)
            {
                for (int c = 1; c <= lastCol; c++)
                {
                    var s = V(ws.Cell(r, c));
                    if (s.Contains("統計期間"))
                    {
                        var years = rxYear.Matches(s).Select(m => int.Parse(m.Value)).ToList();
                        if (years.Count >= 2) { periodStart = years[0]; periodEnd = years[1]; }
                    }
                }
            }

            // --- 解析資料列 ---
            int startRow = Math.Max(headerRow1, headerRow2) + 1;
            var items = new List<ParsedStatItem>();
            var rawRows = 0;

            string curYear = "", curCity = "", curNation = "";

            for (int r = startRow; r <= lastRow; r++)
            {
                ct.ThrowIfCancellationRequested();
                rawRows++;

                string Y() => string.IsNullOrEmpty(V(ws.Cell(r, colYear))) ? curYear : V(ws.Cell(r, colYear));
                string C() => string.IsNullOrEmpty(V(ws.Cell(r, colCity))) ? curCity : V(ws.Cell(r, colCity));
                string N() => string.IsNullOrEmpty(V(ws.Cell(r, colNation))) ? curNation : V(ws.Cell(r, colNation));

                var yearS = Y();
                var cityS = C();
                var natS = N();

                // 結束條件：三鍵皆空 & 行業欄亦空
                bool catsAllEmpty =
                    categoryCols.All(a => string.IsNullOrWhiteSpace(ws.Cell(r, a.Col).GetString())) &&
                    (!totalCol.HasValue || string.IsNullOrWhiteSpace(ws.Cell(r, totalCol.Value).GetString()));
                if (string.IsNullOrWhiteSpace(yearS) &&
                    string.IsNullOrWhiteSpace(cityS) &&
                    string.IsNullOrWhiteSpace(natS) &&
                    catsAllEmpty)
                {
                    break;
                }

                // 向下延展群組鍵
                if (!string.IsNullOrWhiteSpace(yearS)) curYear = yearS;
                if (!string.IsNullOrWhiteSpace(cityS)) curCity = cityS;
                if (!string.IsNullOrWhiteSpace(natS)) curNation = natS;

                if (string.IsNullOrWhiteSpace(curYear) ||
                    string.IsNullOrWhiteSpace(curCity) ||
                    string.IsNullOrWhiteSpace(curNation))
                    continue;

                // 排除合計/總計列
                if (curCity.Contains("合計") || curCity.Contains("總計")) continue;
                if (curNation.Contains("合計") || curNation.Contains("總計")) continue;

                if (!int.TryParse(curYear, out var yearInt)) continue;

                var nationality = MapNationality(curNation);
                if (nationality is null) continue;

                foreach (var (col, zh) in categoryCols)
                {
                    var count = GetInt(ws.Cell(r, col));
                    items.Add(new ParsedStatItem(
                        Year: yearInt,
                        CityName: curCity,
                        Nationality: nationality.Value,
                        CategoryType: CrossCategoryType.Industry,
                        CategoryKey: ToIndustryKey(zh),
                        CategoryNameZh: zh,
                        Count: count,
                        IsTotalRow: false
                    ));
                }

                if (totalCol.HasValue)
                {
                    var total = GetInt(ws.Cell(r, totalCol.Value));
                    items.Add(new ParsedStatItem(
                        Year: yearInt,
                        CityName: curCity,
                        Nationality: nationality.Value,
                        CategoryType: CrossCategoryType.Industry,
                        CategoryKey: "Ind_Total",
                        CategoryNameZh: "總計",
                        Count: total,
                        IsTotalRow: true
                    ));
                }
            }

            var parsed = new ParseResult
            {
                CrossTableTitle = Path.GetFileNameWithoutExtension(xlsxFullPath),
                PeriodYearStart = periodStart ?? (items.Count > 0 ? items.Min(i => i.Year) : (int?)null),
                PeriodYearEnd = periodEnd ?? (items.Count > 0 ? items.Max(i => i.Year) : (int?)null),
                RawRowCount = rawRows,
                ParsedRowCount = items.Count,
                Items = items
            };

            return Task.FromResult(parsed);
        }

        private static int GetInt(IXLCell cell)
        {
            // 優先數值，其次字串轉數值，最後 0
            if (cell.TryGetValue<double>(out var d)) return (int)Math.Round(d);
            var s = cell.GetString();
            if (int.TryParse(s, out var i)) return i;
            if (double.TryParse(s, out d)) return (int)Math.Round(d);
            return 0;
        }

        private static NationalityCode? MapNationality(string s)
        {
            s = s.Replace('－', '-').Trim();
            // 兼容「外國籍-印尼」或僅「印尼」的寫法
            if (s.Contains("印尼")) return NationalityCode.Indonesia;
            if (s.Contains("泰國")) return NationalityCode.Thailand;
            if (s.Contains("越南")) return NationalityCode.Vietnam;
            if (s.Contains("菲律賓")) return NationalityCode.Philippines;
            if (s.Contains("馬來西亞")) return NationalityCode.Malaysia;
            if (s.Contains("其他")) return NationalityCode.OtherCountry;
            return null;
        }

        private static string ToIndustryKey(string zh)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["製造業"] = "Ind_Manufacturing",
                ["營造業"] = "Ind_Construction",
                ["家庭幫傭"] = "Ind_Housemaid",
                ["家庭看護"] = "Ind_Caregiver",
                ["養護機構看護"] = "Ind_NursingHomeCare",
                ["不詳"] = "Ind_Unknown",
                ["其他"] = "Ind_Other"
            };
            if (map.TryGetValue(zh, out var key)) return key;

            var cleaned = new string(zh.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Ind_Unknown" : "Ind_" + cleaned;
        }
    }
}
