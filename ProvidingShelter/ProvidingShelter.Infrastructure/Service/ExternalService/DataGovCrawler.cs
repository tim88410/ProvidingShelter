using System.Net.Http.Headers;
using System.Text;
using HtmlAgilityPack;

namespace ProvidingShelter.Infrastructure.Service.ExternalService;

public sealed class DataGovCrawler
{
    private readonly HttpClient _http;
    private const string Base = "https://data.gov.tw";

    public DataGovCrawler(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ProvidingShelterBot/1.0");
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/html"));
    }

    public async Task<IReadOnlyList<Uri>> SearchDatasetLinksAsync(
    int page = 1, int size = 10, string sort = "_score_desc", string keywordUrlEncoded = "%E6%80%A7%E4%BE%B5",
    CancellationToken ct = default)
    {
        var url = $"{Base}/datasets/search?p={page}&size={size}&s={sort}&rft={keywordUrlEncoded}";
        var html = await _http.GetStringAsync(url, ct);

        var doc = new HtmlDocument();
        doc.OptionFixNestedTags = true;
        doc.LoadHtml(html);

        var results = new List<Uri>();
        var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // [FIX] 僅在 .dataset-list 容器下找連結，避免撈到主選單
        var anchors = doc.DocumentNode.SelectNodes(
            "//ul[contains(concat(' ', normalize-space(@class), ' '), ' dataset-list ')]" +
            "//a[@href and (contains(@href, '/dataset/') or contains(@href, '://data.gov.tw/dataset/'))]"
        );

        void Collect(IEnumerable<HtmlNode> nodes)
        {
            foreach (var a in nodes)
            {
                var href = a.GetAttributeValue("href", "")?.Trim();
                if (string.IsNullOrEmpty(href)) continue;

                // 相對路徑 → 絕對路徑
                var abs = href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : $"{Base}{href}";

                // 只收 /dataset/{數字} 的詳情頁
                if (!abs.Contains("/dataset/", StringComparison.OrdinalIgnoreCase)) continue;

                if (Uri.TryCreate(abs, UriKind.Absolute, out var uri))
                {
                    var seg = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault();
                    if (!string.IsNullOrEmpty(seg) && seg.All(char.IsDigit))
                    {
                        if (dedup.Add(uri.AbsoluteUri))
                            results.Add(uri);
                    }
                }
            }
        }

        if (anchors is not null && anchors.Count > 0)
        {
            Collect(anchors);
        }

        // [FIX] Fallback：若版面改動或抓不到 .dataset-list，就退而求其次
        if (results.Count == 0)
        {
            var alt = doc.DocumentNode.SelectNodes(
                "//a[@href and contains(@href, '/dataset/')]");
            if (alt is not null && alt.Count > 0)
                Collect(alt);
        }

        return results;
    }

    public async Task<DatasetDetailPage> FetchDatasetDetailAsync(Uri datasetUrl, CancellationToken ct = default)
    {
        // 讀 HTML
        var html = await _http.GetStringAsync(datasetUrl, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Title: <h2 class="print-title">...</h2>
        var title = doc.DocumentNode.SelectSingleNode("//h2[contains(@class,'print-title')]")
                   ?.InnerText?.Trim() ?? "";

        // DataId: URL 最後一段數字
        var segments = datasetUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dataId = segments.Last();

        // 取標籤資訊 table-row：以 <strong>內容</strong> 為鍵
        string? GetCellByStrong(string strongText)
        {
            var node = doc.DocumentNode.SelectSingleNode(
                $"//div[contains(@class,'table-row')]//div[contains(@class,'th')]/strong[text()='{strongText}']/ancestor::div[contains(@class,'table-row')]");
            var valNode = node?.SelectSingleNode(".//div[contains(@class,'table-cell')][not(contains(@class,'th'))]");
            return valNode?.InnerText?.Trim();
        }

        // 取 subtitle
        var dataRange = doc.DocumentNode
            .SelectSingleNode("//div[contains(@class,'subtitle')]")?.InnerText?.Trim() ?? "";

        var onshelf = GetCellByStrong("上架日期");                // e.g. 2018-05-04
        var update = GetCellByStrong("詮釋資料更新時間");          // e.g. 2025-06-17 16:55
        var provider = GetCellByStrong("提供機關") ?? "";
        string? dataVersion = GetCellByStrong("資料名稱與版本號"); // 若頁面沒有就 null

        DateOnly? onshelfDate = null;
        if (DateOnly.TryParse(onshelf, out var d)) onshelfDate = d;

        DateTime? updateDate = null;
        if (DateTime.TryParse(update, out var dt)) updateDate = dt;

        // 「資料資源下載網址」區塊：每個 <li class='resource-item'> 裡面有<a href="..."> ，旁邊 <span>顯示資料名
        var resourceItems = new List<DatasetResourceItem>();
        var lis = doc.DocumentNode.SelectNodes("//li[contains(@class,'resource-item')]");
        if (lis != null)
        {
            foreach (var li in lis)
            {
                var a = li.SelectSingleNode(".//a[@href]");
                var span = li.SelectSingleNode(".//span");
                var href = a?.GetAttributeValue("href", "");
                var name = span?.InnerText?.Trim();

                if (!string.IsNullOrWhiteSpace(href) && !string.IsNullOrWhiteSpace(name))
                {
                    var url = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? href
                        : $"{Base}{href}";
                    var ext = TryGetExtensionFromUrl(url);
                    resourceItems.Add(new DatasetResourceItem(name!, ext, url));
                }
            }
        }

        return new DatasetDetailPage(
            DataId: dataId,
            Title: title,
            Link: datasetUrl.ToString(),
            OnshelfDate: onshelfDate,
            UpdateDate: updateDate,
            Provider: StripInnerText(provider),
            DataRange: dataRange,
            DataVersion: dataVersion,
            Resources: resourceItems);
    }

    public async Task<string> DownloadFileAsync(string fileUrl, string downloadDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(downloadDir);
        using var resp = await _http.GetAsync(fileUrl, ct);
        resp.EnsureSuccessStatusCode();

        var fileNameFromUrl = GetFileNameFromUrl(fileUrl);
        var fullPath = Path.Combine(downloadDir, fileNameFromUrl);
        await using var fs = File.Create(fullPath);
        await resp.Content.CopyToAsync(fs, ct);
        return fullPath;
    }

    private static string StripInnerText(string htmlOrText)
    {
        // provider 可能是 <a><span>文字</span></a>；這裡粗略移除標籤
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlOrText);
        return doc.DocumentNode.InnerText.Trim();
    }

    private static string? TryGetExtensionFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var dot = path.LastIndexOf('.');
            return dot >= 0 ? path[(dot + 1)..].ToLowerInvariant() : null;
        }
        catch { return null; }
    }

    private static string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var filename = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
            return string.IsNullOrWhiteSpace(filename) ? $"download_{DateTimeOffset.Now.ToUnixTimeSeconds()}" : filename;
        }
        catch
        {
            return $"download_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        }
    }
}

public record DatasetDetailPage(
    string DataId,
    string Title,
    string Link,
    DateOnly? OnshelfDate,
    DateTime? UpdateDate,
    string Provider,
    string DataRange,
    string? DataVersion,
    IReadOnlyList<DatasetResourceItem> Resources
);

public record DatasetResourceItem(string DataName, string? Extension, string Url);
