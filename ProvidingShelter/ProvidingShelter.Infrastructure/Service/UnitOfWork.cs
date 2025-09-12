using ProvidingShelter.Domain.SeedWork;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Service
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly ShelterDbContext _db;
        public UnitOfWork(ShelterDbContext db) => _db = db;

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }
}
