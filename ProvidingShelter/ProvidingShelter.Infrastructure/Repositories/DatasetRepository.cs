using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.DatasetAggregate;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Repositories;

public class DatasetRepository : IDatasetRepository
{
    private readonly ShelterDbContext _db;

    public DatasetRepository(ShelterDbContext db) => _db = db;

    public Task<Dataset?> FindByDataIdAsync(string dataId, CancellationToken ct) =>
        _db.Datasets
           .Include("_resources")
           .FirstOrDefaultAsync(x => x.DataId == dataId, ct);

    public async Task AddAsync(Dataset dataset, CancellationToken ct) =>
        await _db.Datasets.AddAsync(dataset, ct);

    public Task UpdateAsync(Dataset dataset, CancellationToken ct)
    {
        _db.Datasets.Update(dataset);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
