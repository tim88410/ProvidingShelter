using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProvidingShelter.Common.AppSettings;

namespace ProvidingShelter.Application.Services.Cache
{
    public sealed class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _cache;
        private readonly AnalyticsCacheOptions _opt;

        public MemoryCacheProvider(IMemoryCache cache, IOptions<AnalyticsCacheOptions> opt)
        {
            _cache = cache;
            _opt = opt.Value ?? new AnalyticsCacheOptions();
        }

        public async Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpiration = null,
            CancellationToken ct = default)
        {
            if (!_opt.Enabled)
                return await factory(ct);

            if (_cache.TryGetValue(key, out T value))
                return value;

            var created = await factory(ct);

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(_opt.AbsoluteExpirationMinutes)
            };

            _cache.Set(key, created, options);
            return created;
        }

        public void Remove(string key) => _cache.Remove(key);
    }
}
