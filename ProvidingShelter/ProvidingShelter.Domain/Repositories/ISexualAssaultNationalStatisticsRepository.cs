namespace ProvidingShelter.Domain.Repositories
{
    using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;

    public interface ISexualAssaultNationalStatisticsRepository
    {
        Task BulkInsertAsync(IEnumerable<SexualAssaultStat> items, CancellationToken ct);
    }
}