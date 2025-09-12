using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Infrastructure.Models;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Repositories
{
    public interface IDatasetResourceQueries
    {
        Task<List<SexualAssaultOdsDetail>> GetSexualAssaultOdsDetailAsync(CancellationToken ct = default);
    }

    public class DatasetResourceQueries : IDatasetResourceQueries
    {
        private readonly ShelterDbContext _db;
        public DatasetResourceQueries(ShelterDbContext db) => _db = db;

        public async Task<List<SexualAssaultOdsDetail>> GetSexualAssaultOdsDetailAsync(CancellationToken ct = default)
        {
            var q =
                from c in _db.DatasetResourceContents
                join r in _db.DatasetResources
                    on new { c.DatasetId, c.ResourceKey } equals new { r.DatasetId, r.ResourceKey }
                where EF.Functions.Like(r.FieldDesc, "%OWNERCITYCODE%")
                   && r.DownloadURL != null && r.Title != null
                select new { r.DownloadURL, r.Title };

            return await q
                .AsNoTracking()
                .Distinct() // (DownloadURL, Title) 去重
                .Select(x => new SexualAssaultOdsDetail
                {
                    DownloadURL = x.DownloadURL,  
                    Title = x.Title
                })
                .ToListAsync(ct);
        }
    }
}
