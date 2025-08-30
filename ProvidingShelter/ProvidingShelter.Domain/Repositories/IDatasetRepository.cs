using ProvidingShelter.Domain.Aggregates.DatasetAggregate;

namespace ProvidingShelter.Domain.Repositories;

public interface IDatasetRepository
{
    Task<Dataset?> FindByDataIdAsync(string dataId, CancellationToken ct);
    Task AddAsync(Dataset dataset, CancellationToken ct);
    Task UpdateAsync(Dataset dataset, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
