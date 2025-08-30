using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Application.Queries.Backend;

public sealed class GetDatasetResourcesQuery
{
    private readonly ShelterDbContext _db;
    public GetDatasetResourcesQuery(ShelterDbContext db) => _db = db;

    public Task<List<DatasetResourceVm>> HandleAsync(string dataId, CancellationToken ct = default)
        => _db.DatasetResources
              .Where(r => _db.Datasets.Any(d => d.DataId == dataId && EF.Property<Guid>(r, "DatasetId") == d.Id))
              .Select(r => new DatasetResourceVm
              {
                  DatasetId = dataId,
                  DataName = r.DataName,
                  Extension = r.Extension,
                  FileUrl = r.FileUrl,
                  DownloadedPath = r.DownloadedPath
              }).ToListAsync(ct);
}

public sealed class DatasetResourceVm
{
    public string DatasetId { get; set; } = default!;
    public string DataName { get; set; } = default!;
    public string? Extension { get; set; }
    public string FileUrl { get; set; } = default!;
    public string? DownloadedPath { get; set; }
}
