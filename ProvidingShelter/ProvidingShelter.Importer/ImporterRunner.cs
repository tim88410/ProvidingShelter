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

        public async Task RunAsync(int mode, CancellationToken ct)
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
                            _logger.LogError(ex, "Mode 3: UpsertFromV2Async failed for DatasetId={id}", id);
                        }

                        // Step 2: 抓取 + 轉換 + 落地 + 紀錄大小（若 Step1 失敗，仍可嘗試，但這裡保守直接跳過）
                        if (ok)
                        {
                            try
                            {
                                await _harvester.ProcessAllAllowedAsync(id, ct);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                ok = false;
                                _logger.LogError(ex, "Mode 3: ProcessAllAllowedAsync failed for DatasetId={id}", id);
                            }
                        }

                        sw.Stop();
                        if (ok)
                            _logger.LogDebug("Mode 3: DatasetId={id} done in {ms} ms.", id, sw.ElapsedMilliseconds);
                        else
                            _logger.LogWarning("Mode 3: DatasetId={id} finished with errors in {ms} ms.", id, sw.ElapsedMilliseconds);
                    }

                    _logger.LogInformation("Mode 3: all done. Processed {count} dataset ids.", ids.Count);
                    break;

                default:
                    _logger.LogWarning("Unknown mode {mode}. Use --mode=1|2|3", mode);
                    break;
            }
        }
    }
}
