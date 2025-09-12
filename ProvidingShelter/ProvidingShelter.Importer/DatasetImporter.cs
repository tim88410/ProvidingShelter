using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProvidingShelter.Domain.Aggregates.DatasetAggregate;
using ProvidingShelter.Infrastructure.Persistence;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace ProvidingShelter.Importer
{
    public sealed class DatasetImporter
    {
        private readonly ShelterDbContext _db;
        private readonly IHttpClientFactory _httpFactory;
        private readonly JsonArrayAsyncReader _jsonReader;
        private readonly ILogger<DatasetImporter> _logger;
        private readonly IConfiguration _config;

        public DatasetImporter(
            ShelterDbContext db,
            IHttpClientFactory httpFactory,
            JsonArrayAsyncReader jsonReader,
            ILogger<DatasetImporter> logger,
            IConfiguration config)
        {
            _db = db;
            _httpFactory = httpFactory;
            _jsonReader = jsonReader;
            _logger = logger;
            _config = config;
        }

        public async Task<int> RunAsync(bool useDelta, CancellationToken ct)
        {
            var importer = _config.GetSection("Importer");
            var mode = importer.GetValue<string>("Mode") ?? "http";
            var batchSize = importer.GetValue<int?>("BatchSize") ?? 500;
            var primaryUrl = importer.GetValue<string>(useDelta ? "DeltaJsonUrl" : "FullJsonUrl");
            var localPath = importer.GetValue<string>("LocalJsonPath");

            var existingMap = await _db.Datasets
                .AsNoTracking()
                .Select(x => new { x.DatasetId, x.Id })
                .ToDictionaryAsync(x => x.DatasetId, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);

            var toInserts = new List<Dataset>(batchSize);
            var toUpdates = new List<Dataset>(batchSize);
            var importedAt = DateTime.UtcNow;
            var totalAffected = 0;

            if (mode.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                await using var fs = File.OpenRead(localPath!);
                await foreach (var row in _jsonReader.ReadAsync(fs, ct))
                {
                    UpsertOne(row);
                }
            }
            else
            {
                var client = _httpFactory.CreateClient("opendata");

                var (rows, format) = await TryFetchAndDetectAsync(client, primaryUrl!, ct);

                if (format == PayloadFormat.HtmlOrUnknown)
                {
                    var fallback = FallbackToCsv(primaryUrl!);
                    _logger.LogInformation("Fallback to CSV: {Url}", fallback);
                    rows = await ReadCsvAsync(client, fallback, ct);
                }

                await foreach (var row in rows.WithCancellation(ct))
                {
                    UpsertOne(row);
                }
            }

            if (toInserts.Count > 0)
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = false;
                await _db.Datasets.AddRangeAsync(toInserts, ct);
                totalAffected += await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.AutoDetectChangesEnabled = true;
            }
            if (toUpdates.Count > 0)
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = false;
                totalAffected += await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            _logger.LogInformation("Dataset import done. Affected rows = {Total}", totalAffected);
            return totalAffected;


            void UpsertOne(Dictionary<string, string?> row)
            {
                string? get(params string[] keys)
                {
                    foreach (var k in keys)
                        if (row.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                            return v!.Trim();
                    return null;
                }

                var datasetId = get("資料集識別碼", "dataset_id");
                if (string.IsNullOrWhiteSpace(datasetId)) return;

                var pageUrl = $"https://data.gov.tw/dataset/{datasetId}";
                DateOnly? onshelfDate = DateOnly.TryParse(get("上架日期", "issued"), out var d) ? d : null;
                DateTime? updateDate = DateTime.TryParse(get("詮釋資料更新時間", "modified"), out var dt) ? dt : null;

                // 壓縮去重，避免超長
                var fileFormats = NormalizeList(get("檔案格式"));
                var encoding = NormalizeList(get("編碼格式"));

                var entity = new Dataset(datasetId);
                entity.Upsert(
                    datasetName: get("資料集名稱", "title"),
                    providerAttribute: get("資料提供屬性"),
                    serviceCategory: get("服務分類"),
                    qualityCheck: get("品質檢測"),
                    fileFormats: fileFormats,
                    downloadUrls: get("資料下載網址"),
                    encoding: encoding,
                    publishMethod: get("資料集上架方式"),
                    description: get("資料集描述", "description"),
                    mainFieldDescription: get("主要欄位說明"),
                    provider: get("提供機關", "organization"),
                    updateFrequency: get("更新頻率"),
                    license: get("授權方式", "license"),
                    relatedUrls: get("相關網址"),
                    pricing: get("計費方式"),
                    contactName: get("提供機關聯絡人姓名"),
                    contactPhone: get("提供機關聯絡人電話"),
                    onshelfDate: onshelfDate,
                    updateDate: updateDate,
                    note: get("備註"),
                    pageUrl: pageUrl,
                    importedAt: importedAt
                );

                if (!existingMap.ContainsKey(datasetId))
                {
                    toInserts.Add(entity);
                    if (toInserts.Count >= batchSize)
                    {
                        _db.ChangeTracker.AutoDetectChangesEnabled = false;
                        _db.Datasets.AddRange(toInserts);
                        totalAffected += _db.SaveChanges();
                        _db.ChangeTracker.AutoDetectChangesEnabled = true;

                        foreach (var ins in toInserts)
                            existingMap[ins.DatasetId] = ins.Id;
                        toInserts.Clear();
                    }
                }
                else
                {
                    entity.GetType().GetProperty(nameof(Dataset.Id))!
                          .SetValue(entity, existingMap[datasetId]);

                    _db.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    toUpdates.Add(entity);

                    if (toUpdates.Count >= batchSize)
                    {
                        _db.ChangeTracker.AutoDetectChangesEnabled = false;
                        totalAffected += _db.SaveChanges();
                        _db.ChangeTracker.AutoDetectChangesEnabled = true;
                        toUpdates.Clear();
                    }
                }
            }
        }

        private enum PayloadFormat { Json, Csv, HtmlOrUnknown }

        private async Task<(IAsyncEnumerable<Dictionary<string, string?>>, PayloadFormat)>
            TryFetchAndDetectAsync(HttpClient client, string url, CancellationToken ct)
        {
            HttpResponseMessage? resp = null;
            try
            {
                resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();

                var ctHeader = resp.Content.Headers.ContentType?.ToString();
                var encHeader = string.Join(",", resp.Content.Headers.ContentEncoding ?? Array.Empty<string>());
                _logger.LogInformation("Content-Type: {ct}; Content-Encoding: {enc}",
                    string.IsNullOrWhiteSpace(ctHeader) ? "(none)" : ctHeader,
                    string.IsNullOrWhiteSpace(encHeader) ? "(none)" : encHeader);

                var raw = await resp.Content.ReadAsStreamAsync(ct);
                var s = WrapDecompression(raw, resp);

                var media = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";

                if (media.Contains("json") || url.EndsWith("/json", StringComparison.OrdinalIgnoreCase))
                {
                    return (_jsonReader.ReadAsync(s, ct), PayloadFormat.Json);
                }
                if (media.Contains("csv") || url.EndsWith("/csv", StringComparison.OrdinalIgnoreCase) || media.Contains("text/plain"))
                {
                    return (ReadCsvStreamAsync(s, ct), PayloadFormat.Csv);
                }

                return (ReadEmptyAsync(), PayloadFormat.HtmlOrUnknown);
            }
            finally
            {
            }
        }

        private static string FallbackToCsv(string url)
        {
            if (url.EndsWith("/json", StringComparison.OrdinalIgnoreCase))
                return url.Substring(0, url.Length - 4) + "csv";
            if (url.Contains("format=json", StringComparison.OrdinalIgnoreCase))
                return Regex.Replace(url, "format=json", "format=csv", RegexOptions.IgnoreCase);
            return "https://data.gov.tw/datasets/export/csv";
        }

        private async Task<IAsyncEnumerable<Dictionary<string, string?>>> ReadCsvAsync(HttpClient client, string url, CancellationToken ct)
        {
            var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var raw = await resp.Content.ReadAsStreamAsync(ct);
            var s = WrapDecompression(raw, resp);
            return ReadCsvStreamAsync(s, ct);
        }

        private async IAsyncEnumerable<Dictionary<string, string?>> ReadCsvStreamAsync(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null,
                IgnoreBlankLines = true,
                Delimiter = ","
            };
            using var csv = new CsvReader(reader, cfg);

            if (!await csv.ReadAsync()) yield break;
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();

            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in headers)
                {
                    dict[h] = csv.GetField(h);
                }
                yield return dict;
            }
        }

        private static async IAsyncEnumerable<Dictionary<string, string?>> ReadEmptyAsync()
        {
            await Task.CompletedTask;
            yield break;
        }

        private static Stream WrapDecompression(Stream raw, HttpResponseMessage resp)
        {
            var enc = string.Join(",", resp.Content.Headers.ContentEncoding ?? Array.Empty<string>()).ToLowerInvariant();
            if (enc.Contains("br")) return new BrotliStream(raw, CompressionMode.Decompress, leaveOpen: false);
            if (enc.Contains("gzip")) return new GZipStream(raw, CompressionMode.Decompress, leaveOpen: false);
            if (enc.Contains("deflate")) return new DeflateStream(raw, CompressionMode.Decompress, leaveOpen: false);
            return raw;
        }

        private static string? NormalizeList(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var tokens = input
                .Split(new[] { ';', ',', '|', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return string.Join(";", tokens);
        }
    }
}
