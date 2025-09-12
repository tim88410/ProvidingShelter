using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Service.DomainService
{
    public interface ICityCodeSyncService
    {
        Task<(int updatedRis, int updatedStat)> SyncLatestByCityNameAsync(CancellationToken ct = default);
    }

    public sealed class CityCodeSyncService : ICityCodeSyncService
    {
        private readonly ShelterDbContext _db;
        private readonly ILogger<CityCodeSyncService> _log;

        public CityCodeSyncService(ShelterDbContext db, ILogger<CityCodeSyncService> log)
        {
            _db = db;
            _log = log;
        }

        public async Task<(int updatedRis, int updatedStat)> SyncLatestByCityNameAsync(CancellationToken ct = default)
        {
            var hasStat = await _db.SexualAssaultStats.AsNoTracking().AnyAsync(ct);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Step A：依 CityName 取數值最大 CityCode，重算 IsCurrent（1 / 0）
            var updateRisSql = @"
;WITH ranked AS (
  SELECT CityName,
         CityCode,
         rn = ROW_NUMBER() OVER (
                PARTITION BY CityName
                ORDER BY COALESCE(TRY_CONVERT(int, CityCode), -1) DESC, CityCode DESC
              )
  FROM dbo.RisCityCode
)
UPDATE R
   SET IsCurrent = CASE WHEN ranked.rn = 1 THEN 1 ELSE 0 END
FROM dbo.RisCityCode AS R
JOIN ranked
  ON R.CityCode = ranked.CityCode
 AND R.CityName = ranked.CityName;";

            var updatedRis = await _db.Database.ExecuteSqlRawAsync(updateRisSql, ct);

            var updatedStat = 0;
            if (hasStat)
            {
                var updateStatSql = @"
;WITH winners AS (
  SELECT CityName,
         CityCode,
         rn = ROW_NUMBER() OVER (
                PARTITION BY CityName
                ORDER BY COALESCE(TRY_CONVERT(int, CityCode), -1) DESC, CityCode DESC
              )
  FROM dbo.RisCityCode
),
current_only AS (
  SELECT CityName, CityCode FROM winners WHERE rn = 1
)
UPDATE S
   SET S.CityCode = C.CityCode
FROM dbo.SexualAssaultStat AS S
JOIN current_only AS C
  ON S.CityName = C.CityName
WHERE S.CityCode IS NULL OR S.CityCode <> C.CityCode;";

                updatedStat = await _db.Database.ExecuteSqlRawAsync(updateStatSql, ct);
            }

            await tx.CommitAsync(ct);

            _log.LogInformation("CityCode 同步完成：RisCityCode 更新={UpdatedRis}，SexualAssaultStat 更新={UpdatedStat}", updatedRis, updatedStat);
            return (updatedRis, updatedStat);
        }
    }
}
