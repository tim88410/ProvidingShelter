namespace ProvidingShelter.Application.Services.Cache
{
    public interface ICacheProvider
    {
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpiration = null,
            CancellationToken ct = default);

        void Remove(string key);
    }
}
