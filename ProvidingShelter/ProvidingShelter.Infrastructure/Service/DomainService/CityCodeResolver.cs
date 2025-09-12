using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Infrastructure.Persistence;

namespace ProvidingShelter.Infrastructure.Service.DomainService
{
    public interface ICityCodeResolver
    {
        /// <summary>
        /// 依城市名稱（例：臺北市/台北市）回傳 (CityCode, CityName 正規化)。
        /// 僅匹配 IsCurrent = true 的代碼。
        /// </summary>
        Task<(string? CityCode, string NormalizedCityName)> ResolveAsync(string rawCityName, CancellationToken ct = default);
    }

    public sealed class CityCodeResolver : ICityCodeResolver
    {
        private readonly ShelterDbContext _db;

        public CityCodeResolver(ShelterDbContext db) => _db = db;

        public async Task<(string? CityCode, string NormalizedCityName)> ResolveAsync(string rawCityName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawCityName))
                return (null, rawCityName);

            // 正規化：「台」→「臺」
            var normalized = rawCityName.Replace('台', '臺').Trim();

            var hit = await _db.RisCityCodes
                .AsNoTracking()
                .Where(x => x.IsCurrent == true && x.CityName == normalized)
                .Select(x => new { x.CityCode, x.CityName })
                .FirstOrDefaultAsync(ct);

            if (hit != null)
                return (hit.CityCode, hit.CityName);

            return (null, normalized);
        }
    }
}
