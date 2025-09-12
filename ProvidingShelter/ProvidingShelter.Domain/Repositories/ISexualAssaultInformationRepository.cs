using ProvidingShelter.Domain.Entities;

namespace ProvidingShelter.Domain.Repositories
{
    public interface ISexualAssaultInformationRepository
    {
        Task AddRangeAsync(IEnumerable<SexualAssaultInformation> items, CancellationToken ct = default);
    }
}
