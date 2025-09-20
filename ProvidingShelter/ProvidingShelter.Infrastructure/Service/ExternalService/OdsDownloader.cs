namespace ProvidingShelter.Infrastructure.Service.ExternalService
{
    public interface IOdsDownloader
    {
        Task<Stream> DownloadAsync(string url, CancellationToken ct = default);
    }

    public class OdsDownloader : IOdsDownloader
    {
        private readonly HttpClient _http;

        public OdsDownloader(HttpClient http)
        {
            _http = http;
        }

        public async Task<Stream> DownloadAsync(string url, CancellationToken ct = default)
        {
            var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var ms = new MemoryStream();
            await resp.Content.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }
    }
}
