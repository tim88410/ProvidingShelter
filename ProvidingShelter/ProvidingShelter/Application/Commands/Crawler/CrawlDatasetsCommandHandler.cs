using ProvidingShelter.Domain.Aggregates.DatasetAggregate;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Service.ExternalService;

namespace ProvidingShelter.Application.Commands.Crawler;

public sealed class CrawlDatasetsCommandHandler
{
    private readonly DataGovCrawler _crawler;
    private readonly IDatasetRepository _repo;
    private readonly ILogger<CrawlDatasetsCommandHandler> _logger;
    private readonly string _downloadDir;

    public CrawlDatasetsCommandHandler(
        DataGovCrawler crawler,
        IDatasetRepository repo,
        ILogger<CrawlDatasetsCommandHandler> logger,
        IConfiguration config)
    {
        _crawler = crawler;
        _repo = repo;
        _logger = logger;
        _downloadDir = config.GetValue<string>("App:DownloadDir") ?? "downloads";
    }

    public async Task<int> HandleAsync(CrawlDatasetsCommand cmd, CancellationToken ct = default)
    {
        var datasetLinks = await _crawler.SearchDatasetLinksAsync(
            cmd.Page, cmd.Size, cmd.Sort, cmd.KeywordUrlEncoded, ct);

        var upserted = 0;

        foreach (var link in datasetLinks)
        {
            var detail = await _crawler.FetchDatasetDetailAsync(link, ct);

            var existed = await _repo.FindByDataIdAsync(detail.DataId, ct);
            Dataset entity;
            if (existed is null)
            {
                entity = new Dataset(
                    dataId: detail.DataId,
                    title: detail.Title,
                    link: detail.Link,
                    onshelfDate: detail.OnshelfDate,
                    updateDate: detail.UpdateDate,
                    provider: detail.Provider,
                    dataRange: detail.DataRange,
                    dataVersion: detail.DataVersion
                );

                foreach (var r in detail.Resources)
                    entity.UpsertResource(r.DataName, r.Extension, r.Url);

                await _repo.AddAsync(entity, ct);
                upserted++;
            }
            else
            {
                // 若內容變更則更新
                entity = existed;
                // 簡化處理：直接覆蓋基本欄位
                entity = new Dataset(
                    entity.DataId,
                    detail.Title,
                    detail.Link,
                    detail.OnshelfDate,
                    detail.UpdateDate,
                    detail.Provider,
                    detail.DataRange,
                    detail.DataVersion);

                // 重灌資源（或可做更精緻的 diff）
                foreach (var r in detail.Resources)
                    entity.UpsertResource(r.DataName, r.Extension, r.Url);

                await _repo.UpdateAsync(entity, ct);
            }

            // 可選：下載
            if (cmd.DownloadFiles)
            {
                foreach (var r in entity.Resources)
                {
                    try
                    {
                        var local = await _crawler.DownloadFileAsync(r.FileUrl,
                            Path.Combine(_downloadDir, entity.DataId), ct);
                        r.MarkDownloaded(local);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Download failed: {Url}", r.FileUrl);
                    }
                }
            }

            await _repo.SaveChangesAsync(ct);
            await Task.Delay(300, ct); // 禮貌性延遲，避免打太兇
        }

        return upserted;
    }
}
