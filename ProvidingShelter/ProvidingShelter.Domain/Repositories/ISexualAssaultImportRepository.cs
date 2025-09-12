namespace ProvidingShelter.Domain.Repositories
{
    using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;

    public interface ISexualAssaultImportRepository
    {
        Task AddAsync(SexualAssaultImport entity, CancellationToken ct);
        Task<SexualAssaultImport?> FindByHashAsync(string sha256, CancellationToken ct);
        Task UpdateAsync(SexualAssaultImport entity, CancellationToken ct);
    }
}
