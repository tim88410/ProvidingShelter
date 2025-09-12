using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Repositories
{
    public sealed class SexualAssaultImportRepository : ISexualAssaultImportRepository
    {
        private readonly ShelterDbContext _db;
        public SexualAssaultImportRepository(ShelterDbContext db) => _db = db;

        public async Task AddAsync(SexualAssaultImport entity, CancellationToken ct)
        {
            await _db.SexualAssaultImports.AddAsync(entity, ct);
        }

        public Task<SexualAssaultImport?> FindByHashAsync(string sha256, CancellationToken ct)
        {
            return _db.SexualAssaultImports
                      .AsNoTracking()
                      .FirstOrDefaultAsync(x => x.FileHashSha256 == sha256, ct);
        }

        public Task UpdateAsync(SexualAssaultImport entity, CancellationToken ct)
        {
            _db.SexualAssaultImports.Update(entity);
            return Task.CompletedTask;
        }
    }
}
