using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProvidingShelter.Infrastructure.Persistence;
using ProvidingShelter.Infrastructure.Persistence.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ProvidingShelter.Importer.Pipeline
{
    public class ResourceHarvester
    {
        private readonly ShelterDbContext _db;
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<ResourceHarvester> _logger;
        private readonly StorageOptions _storage;
        private readonly FormatOptions _formats;
        private readonly ResourceRegistry _registry;
        private readonly IJsonUtil _json;

        public ResourceHarvester(
            ShelterDbContext db,
            IHttpClientFactory factory,
            IOptions<StorageOptions> storage,
            IOptions<FormatOptions> formats,
            ResourceRegistry registry,
            IJsonUtil json,
            ILogger<ResourceHarvester> logger)
        {
            _db = db;
            _factory = factory;
            _logger = logger;
            _storage = storage.Value;
            _formats = formats.Value;
            _registry = registry;
            _json = json;
        }

        // =============== helpers (case-insensitive) ===============
        private static bool TryGetPropertyValueCI(JsonObject obj, string key, out JsonNode? value)
        {
            if (obj.TryGetPropertyValue(key, out value) && value is not null) return true;
            foreach (var kv in obj)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return value is not null;
                }
            }
            value = null;
            return false;
        }

        private string? FirstString(JsonObject? obj, params string[] keys)
        {
            if (obj == null) return null;
            foreach (var k in keys)
            {
                if (TryGetPropertyValueCI(obj, k, out var n) && n is not null)
                {
                    try
                    {
                        if (n is JsonValue v)
                        {
                            if (v.TryGetValue<string>(out var s)) return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
                            return v.ToString();
                        }
                        if (n is JsonObject or JsonArray)
                        {
                            // ★ 對 Json 物件/陣列，用不逃逸的 ToJsonString
                            return n.ToJsonString(_json.Unescaped);
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            return null;
        }

        private static JsonArray? FirstArray(JsonObject? obj, params string[] keys)
        {
            if (obj == null) return null;
            foreach (var k in keys)
            {
                if (TryGetPropertyValueCI(obj, k, out var n) && n is JsonArray arr) return arr;
            }
            return null;
        }

        // 找不到時，做一個淺層遞迴深搜（最多兩層）去找第一個名稱符合的 JsonArray
        private static JsonArray? FindArrayDeep(JsonObject root, params string[] keys)
        {
            foreach (var (k, v) in root)
            {
                if (keys.Any(t => string.Equals(t, k, StringComparison.OrdinalIgnoreCase)) && v is JsonArray arr)
                    return arr;
                if (v is JsonObject o)
                {
                    // 一層向下
                    foreach (var (k2, v2) in o)
                    {
                        if (keys.Any(t => string.Equals(t, k2, StringComparison.OrdinalIgnoreCase)) && v2 is JsonArray arr2)
                            return arr2;
                    }
                }
            }
            return null;
        }

        public async Task UpsertFromV2Async(string datasetId, CancellationToken ct)
        {
            try
            {
                // 讀 v2 詳情
                var detailDoc = await new V2DatasetClient(_factory).GetDetailAsync(datasetId, ct);
                if (detailDoc == null)
                {
                    _logger.LogWarning("v2 detail not found: {id}", datasetId);
                    return;
                }

                // 轉 JsonObject 方便容錯處理
                JsonObject? root;
                try
                {
                    root = JsonNode.Parse(detailDoc.RootElement.GetRawText()) as JsonObject;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dataset {id}: invalid JSON.", datasetId);
                    return;
                }
                if (root is null)
                {
                    _logger.LogWarning("Dataset {id}: empty JSON.", datasetId);
                    return;
                }

                // 有些會回 success=false
                if (TryGetPropertyValueCI(root, "success", out var succNode) &&
                    succNode is JsonValue sv &&
                    sv.TryGetValue<bool>(out var ok) &&
                    ok == false)
                {
                    var msg = root["message"]?.ToString();
                    _logger.LogWarning("Dataset {id}: v2 success=false, message={msg}", datasetId, msg);
                    return;
                }

                // ★ 先取 result，沒有才退回 root
                var datasetObj = (root["result"] as JsonObject) ?? root;

                // ★ distribution/resources 陣列鍵名可能不同
                var distArr = FirstArray(datasetObj, "distribution", "distributions", "resources");
                if (distArr is null || distArr.Count == 0)
                {
                    _logger.LogInformation("Dataset {id}: no distributions/resources found.", datasetId);
                    return;
                }

                // 英文資源陣列（若存在）
                var enResArr = datasetObj?["en"]?["resources"] as JsonArray;

                var now = DateTime.UtcNow;
                var list = new List<DatasetResource>();
                int idx = 0; // 0-based ResourceKey

                foreach (var n in distArr)
                {
                    ct.ThrowIfCancellationRequested();
                    if (n is not JsonObject resObj) { idx++; continue; }

                    // ★ 加入 resource* 系列鍵名
                    var resourceId = FirstString(resObj, "resourceId", "identifier", "id");
                    var title = FirstString(resObj, "resourceDescription", "title", "name", "resourceName");
                    var desc = FirstString(resObj, "resourceDescription", "description", "note");

                    // resourceField 是陣列，FirstString 會以 ToJsonString() 回傳字串；保留原樣存入 FieldDesc
                    var fieldDesc = FirstString(resObj, "fieldDesc", "resourceField", "fields", "schema");

                    var format = FirstString(resObj, "resourceFormat", "format", "filetype");
                    var mediaType = FirstString(resObj, "resourceMediaType", "mediaType", "mimetype", "type");
                    var accessUrl = FirstString(resObj, "resourceAccessUrl", "accessURL", "accessUrl", "url", "landingPage");
                    var downloadUrl = FirstString(resObj, "resourceDownloadUrl", "downloadURL", "downloadUrl", "href");

                    string? enDescription = null;
                    string? enFieldDesc = null;
                    if (enResArr != null && enResArr.Count > idx && enResArr[idx] is JsonObject enObj)
                    {
                        enDescription = FirstString(enObj, "description", "note");
                        enFieldDesc = FirstString(enObj, "fieldDesc", "fields", "schema");
                    }

                    var entity = new DatasetResource
                    {
                        DatasetId = datasetId,
                        ResourceKey = $"{datasetId}_{idx}",
                        Title = title,
                        Description = desc,
                        DescriptionEn = enDescription,
                        FieldDesc = fieldDesc,
                        FieldDescEn = enFieldDesc,
                        Format = format,
                        MediaType = mediaType,
                        AccessURL = accessUrl,
                        DownloadURL = downloadUrl,
                        IsApiLike = IsApiLike(format, mediaType, downloadUrl ?? accessUrl),
                        UpdatedAtUtc = now
                    };

                    list.Add(entity);
                    idx++;
                }

                // Upsert（以 (DatasetId, ResourceKey) 當主鍵）
                foreach (var r in list)
                {
                    var tracked = await _db.DatasetResources.FindAsync(new object[] { r.DatasetId, r.ResourceKey }, ct);
                    if (tracked == null)
                    {
                        _db.DatasetResources.Add(r);
                    }
                    else
                    {
                        tracked.Title = r.Title;
                        tracked.Description = r.Description;
                        tracked.DescriptionEn = r.DescriptionEn;
                        tracked.FieldDesc = r.FieldDesc;
                        tracked.FieldDescEn = r.FieldDescEn;
                        tracked.Format = r.Format;
                        tracked.MediaType = r.MediaType;
                        tracked.AccessURL = r.AccessURL;
                        tracked.DownloadURL = r.DownloadURL;
                        tracked.IsApiLike = r.IsApiLike;
                        tracked.UpdatedAtUtc = now;
                    }
                }

                await _db.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertFromV2Async failed for DatasetId={id}", datasetId);
                // 不拋出，讓批次續跑
            }
        }

        public async Task ProcessAllAllowedAsync(string datasetId, CancellationToken ct)
        {
            var resList = await _db.DatasetResources
                .Where(x => x.DatasetId == datasetId)
                .AsNoTracking().ToListAsync(ct);

            foreach (var r in resList)
            {
                if (!IsAllowedFormat(r)) { await MarkSkippedAsync(r, "Denied format", ct); continue; }
                await ProcessOneAsync(r, ct);
            }
        }

        private bool IsAllowedFormat(DatasetResource r)
        {
            var f = (r.Format ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(f))
            {
                f = GuessFormatFromUrl(r.DownloadURL ?? r.AccessURL ?? "");
            }
            if (string.IsNullOrWhiteSpace(f)) return false;
            if (_formats.Deny.Contains(f)) return false;
            if (_formats.Container.Contains(f)) return true;
            return _formats.Allow.Contains(f);
        }

        private static string GuessFormatFromUrl(string url)
        {
            try
            {
                var u = new Uri(url);
                var seg = u.Segments.LastOrDefault() ?? "";
                var dot = seg.LastIndexOf('.');
                if (dot > 0) return seg[(dot + 1)..].ToUpperInvariant();
            }
            catch { }
            return "";
        }

        private static bool IsApiLike(string? format, string? mediaType, string? url)
        {
            var u = (url ?? "").ToLowerInvariant();
            var f = (format ?? "").ToLowerInvariant();
            var m = (mediaType ?? "").ToLowerInvariant();
            return u.Contains("/api/") || u.Contains("?format=json") || f.Contains("api") || f.Contains("json") || m.Contains("json") || m.Contains("xml");
        }

        private static bool IsApiLike(DatasetResource r)
            => IsApiLike(r.Format, r.MediaType, r.DownloadURL ?? r.AccessURL);

        private async Task MarkSkippedAsync(DatasetResource r, string reason, CancellationToken ct)
        {
            var fetch = new DatasetResourceFetch
            {
                DatasetId = r.DatasetId,
                ResourceKey = r.ResourceKey,
                FetchAtUtc = DateTime.UtcNow,
                Ok = false,
                Error = $"Skipped: {reason}",
                DetectedFormat = r.Format
            };
            _db.DatasetResourceFetches.Add(fetch);
            var tracked = await _db.DatasetResources.FindAsync(new object[] { r.DatasetId, r.ResourceKey }, ct);
            if (tracked != null) { tracked.Status = 2; tracked.UpdatedAtUtc = DateTime.UtcNow; }
            await _db.SaveChangesAsync(ct);
        }

        private async Task ProcessOneAsync(DatasetResource r, CancellationToken ct)
        {
            var http = _factory.CreateClient("opendata");
            var url = r.DownloadURL ?? r.AccessURL;
            if (string.IsNullOrWhiteSpace(url)) { await MarkSkippedAsync(r, "No URL", ct); return; }

            var fetch = new DatasetResourceFetch
            {
                DatasetId = r.DatasetId,
                ResourceKey = r.ResourceKey,
                FetchAtUtc = DateTime.UtcNow
            };
            _db.DatasetResourceFetches.Add(fetch);

            try
            {
                using var head = new HttpRequestMessage(HttpMethod.Head, url);
                using var headResp = await http.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!headResp.IsSuccessStatusCode && (int)headResp.StatusCode != 405)
                {
                    fetch.HttpStatus = (int)headResp.StatusCode;
                    fetch.Ok = false;
                    fetch.Error = $"HEAD {headResp.StatusCode}";
                    await _db.SaveChangesAsync(ct);
                    return;
                }

                var contentType = headResp.Content?.Headers?.ContentType?.ToString();
                var enc = string.Join(',', headResp.Content?.Headers?.ContentEncoding ?? Array.Empty<string>());
                var etag = headResp.Headers.ETag?.Tag;
                var lastModified = headResp.Content?.Headers?.LastModified?.UtcDateTime;
                var len = headResp.Content?.Headers?.ContentLength;

                fetch.ContentType = contentType;
                fetch.Encoding = enc;
                fetch.ETag = etag;
                fetch.LastModified = lastModified;
                fetch.HttpStatus = headResp.IsSuccessStatusCode ? 200 : (int?)headResp.StatusCode;
                fetch.WireSizeBytes = len;

                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                fetch.HttpStatus = (int)resp.StatusCode;
                if (!resp.IsSuccessStatusCode)
                {
                    fetch.Ok = false;
                    fetch.Error = $"GET {resp.StatusCode}";
                    await _db.SaveChangesAsync(ct);
                    return;
                }

                var baseDir = Path.Combine(_storage.RootPath, r.DatasetId, r.ResourceKey);
                Directory.CreateDirectory(baseDir);

                var fileName = TryGetFileName(resp) ?? $"{r.ResourceKey}";
                var fullPath = Path.Combine(baseDir, fileName);

                await using (var fs = File.Create(fullPath))
                {
                    await resp.Content.CopyToAsync(fs, ct);
                }

                var savedSize = new FileInfo(fullPath).Length;
                fetch.SavedPath = fullPath;
                fetch.SavedSizeBytes = savedSize;

                var tracked = await _db.DatasetResources.FindAsync(new object[] { r.DatasetId, r.ResourceKey }, ct);
                if (tracked != null)
                {
                    tracked.LastKnownWireSizeBytes = fetch.WireSizeBytes;
                    tracked.LastKnownSavedSizeBytes = fetch.SavedSizeBytes;
                    tracked.LastModified = lastModified;
                    tracked.ETag = etag;
                }

                var ctx = new ResourceContext
                {
                    DatasetId = r.DatasetId,
                    ResourceKey = r.ResourceKey,
                    SourceUrl = url!,
                    ContentType = contentType,
                    Format = (r.Format ?? GuessFormatFromUrl(url!)).ToUpperInvariant(),
                    LocalPath = fullPath
                };

                var (converter, json) = await _registry.TryConvertAsync(ctx, ct);
                fetch.Converter = converter;
                fetch.DetectedFormat = ctx.Format;

                var contentRow = await _db.DatasetResourceContents.FindAsync(new object[] { r.DatasetId, r.ResourceKey }, ct)
                                 ?? new DatasetResourceContent { DatasetId = r.DatasetId, ResourceKey = r.ResourceKey };

                contentRow.WireSizeBytes = fetch.WireSizeBytes;
                contentRow.SavedSizeBytes = fetch.SavedSizeBytes;
                contentRow.ConvertedAtUtc = DateTime.UtcNow;

                if (json != null && Encoding.UTF8.GetByteCount(json) <= _storage.ContentInDbMaxBytes)
                {
                    contentRow.StorageMode = "DB";
                    contentRow.ContentJson = json;
                    contentRow.JsonSizeBytes = Encoding.UTF8.GetByteCount(json);
                    contentRow.ContentPath = null;
                    contentRow.ContentHash = Sha256(json);
                }
                else
                {
                    contentRow.StorageMode = "File";
                    contentRow.ContentPath = fullPath;
                    contentRow.ContentJson = null;
                    contentRow.JsonSizeBytes = null;
                    contentRow.ContentHash = Sha256OfFile(fullPath);
                }

                if (_db.Entry(contentRow).State == EntityState.Detached)
                    _db.DatasetResourceContents.Add(contentRow);

                if (tracked != null) { tracked.Status = 1; tracked.UpdatedAtUtc = DateTime.UtcNow; }
                fetch.Ok = true;
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                fetch.Ok = false;
                fetch.Error = ex.ToString();
                var tracked = await _db.DatasetResources.FindAsync(new object[] { r.DatasetId, r.ResourceKey }, ct);
                if (tracked != null) { tracked.Status = 3; tracked.UpdatedAtUtc = DateTime.UtcNow; }
                await _db.SaveChangesAsync(ct);
            }
        }

        private static string? TryGetFileName(HttpResponseMessage resp)
        {
            if (resp.Content.Headers.ContentDisposition?.FileNameStar != null)
                return Sanitize(resp.Content.Headers.ContentDisposition.FileNameStar);
            if (resp.Content.Headers.ContentDisposition?.FileName != null)
                return Sanitize(resp.Content.Headers.ContentDisposition.FileName.Trim('"'));

            if (resp.RequestMessage?.RequestUri != null)
            {
                var seg = resp.RequestMessage.RequestUri.Segments.LastOrDefault();
                if (!string.IsNullOrEmpty(seg)) return Sanitize(seg);
            }
            return null;

            static string Sanitize(string s)
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                    s = s.Replace(c, '_');
                return s;
            }
        }

        private static string Sha256(string text)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(hash);
        }

        private static string Sha256OfFile(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash);
        }
    }
}
