using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProvidingShelter.Infrastructure.Persistence;
using System.Diagnostics;

namespace ProvidingShelter.Importer
{
    using ProvidingShelter.Importer.Pipeline;

    public class ImporterRunner
    {
        private readonly IConfiguration _config;
        private readonly DatasetImporter _dataset;
        private readonly ShelterDbContext _db;
        private readonly ResourceHarvester _harvester;
        private readonly ILogger<ImporterRunner> _logger;

        public ImporterRunner(
            IConfiguration config,
            DatasetImporter dataset,
            ShelterDbContext db,
            ResourceHarvester harvester,
            ILogger<ImporterRunner> logger)
        {
            _config = config; _dataset = dataset; _db = db; _harvester = harvester; _logger = logger;
        }

        /// <summary>
        /// mode:
        /// 1 - Full catalog import (CSV)
        /// 2 - Delta import
        /// 3 - 全部 DatasetId 逐一處理（v2 詳情 + 允許格式處理/下載）
        /// 4 - 「依 DatasetName 關鍵字」過濾後才處理（v2 詳情 + 僅允許格式；不下載非允許格式、也不計算其容量）
        /// </summary>
        public async Task RunAsync(int mode, string? keyword, CancellationToken ct)
        {
            switch (mode)
            {
                case 1:
                    _logger.LogInformation("Mode 1: Full catalog import (CSV)");
                    await _dataset.RunAsync(useDelta: false, ct);
                    break;

                case 2:
                    _logger.LogInformation("Mode 2: Delta import (changed list, by date window)");
                    await _dataset.RunAsync(useDelta: true, ct);
                    break;

                case 3:
                    _logger.LogInformation("Mode 3: For each DatasetId -> v2 detail -> map distributions -> fetch/convert");

                    // 來源：Datasets 裡所有 DatasetId
                    var ids = await _db.Datasets
                        .AsNoTracking()
                        .Select(x => x.DatasetId)
                        .Distinct()
                        .ToListAsync(ct);

                    if (ids.Count == 0)
                    {
                        _logger.LogWarning("Mode 3: no DatasetId found in table Datasets.");
                        return;
                    }

                    _logger.LogInformation("Mode 3: total {count} dataset ids to process.", ids.Count);

                    await ProcessIdsAsync(ids, downloadUnknown: true, ct);
                    break;

                case 4:
                    _logger.LogInformation("Mode 4: Keyword filtered harvest (v2 detail + allowed formats only; no download for unknown).");

                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        _logger.LogWarning("Mode 4: --keyword=關鍵字 未提供，無法篩選 DatasetName。");
                        return;
                    }

                    // 依 DatasetName LIKE '%keyword%' 篩選 DatasetId
                    var filteredIds = await _db.Datasets
                        .AsNoTracking()
                        .Where(x => x.DatasetName != null && EF.Functions.Like(x.DatasetName!, $"%{keyword}%"))
                        .Select(x => x.DatasetId)
                        .Distinct()
                        .ToListAsync(ct);

                    if (filteredIds.Count == 0)
                    {
                        _logger.LogWarning("Mode 4: 找不到符合關鍵字「{kw}」的資料集。", keyword);
                        return;
                    }

                    _logger.LogInformation("Mode 4: keyword={kw}, matched {count} dataset ids.", keyword, filteredIds.Count);

                    // 重點：downloadUnknown=false -> 不下載非允許格式，也不統計其容量
                    await ProcessIdsAsync(filteredIds, downloadUnknown: false, ct);
                    break;

                default:
                    _logger.LogWarning("Unknown mode {mode}. Use --mode=1|2|3|4", mode);
                    break;
            }
        }

        private async Task ProcessIdsAsync(IReadOnlyList<string> ids, bool downloadUnknown, CancellationToken ct)
        {
            int i = 0;
            foreach (var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                i++;

                // 每 50 筆或第一筆時報告一次進度
                if (i == 1 || i % 50 == 0)
                    _logger.LogInformation("Processing {i}/{n}: {id}", i, ids.Count, id);

                var sw = Stopwatch.StartNew();
                var ok = true;

                // Step 1: 建立/更新 mapping（v2 詳情）
                try
                {
                    await _harvester.UpsertFromV2Async(id, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    ok = false;
                    _logger.LogError(ex, "UpsertFromV2Async failed for DatasetId={id}", id);
                }

                // Step 2: 僅處理允許格式；是否下載未知/不允許格式由 downloadUnknown 控制
                if (ok)
                {
                    try
                    {
                        // 新增的 overload：downloadUnknown=false 時，不下載非允許格式、也不計算容量
                        await _harvester.ProcessAllAllowedAsync(id, ct, downloadUnknown: downloadUnknown);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        ok = false;
                        _logger.LogError(ex, "ProcessAllAllowedAsync failed for DatasetId={id}", id);
                    }
                }

                sw.Stop();
                if (ok)
                    _logger.LogDebug("DatasetId={id} done in {ms} ms.", id, sw.ElapsedMilliseconds);
                else
                    _logger.LogWarning("DatasetId={id} finished with errors in {ms} ms.", id, sw.ElapsedMilliseconds);
            }

            _logger.LogInformation("Processed {count} dataset ids.", ids.Count);
        }
    }
}
