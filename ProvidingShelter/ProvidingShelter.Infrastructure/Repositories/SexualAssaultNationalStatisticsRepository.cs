using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProvidingShelter.Infrastructure.Repositories
{
    public sealed class SexualAssaultNationalStatisticsRepository : ISexualAssaultNationalStatisticsRepository
    {
        private readonly ShelterDbContext _db;
        public SexualAssaultNationalStatisticsRepository(ShelterDbContext db) => _db = db;

        public async Task BulkInsertAsync(IEnumerable<SexualAssaultStat> items, CancellationToken ct)
        {
            // 先用標準 AddRange；之後如需提速可改用 EFCore.BulkExtensions
            await _db.SexualAssaultStats.AddRangeAsync(items, ct);
        }
    }
}
