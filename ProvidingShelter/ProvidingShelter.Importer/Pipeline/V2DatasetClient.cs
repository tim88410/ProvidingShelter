using System.Net.Http.Json;
using System.Text.Json;

namespace ProvidingShelter.Importer.Pipeline
{
    public class V2DatasetClient
    {
        private readonly IHttpClientFactory _factory;
        public V2DatasetClient(IHttpClientFactory factory) => _factory = factory;

        public async Task<JsonDocument?> GetDetailAsync(string datasetId, CancellationToken ct)
        {
            var http = _factory.CreateClient("opendata");
            var url = $"https://data.gov.tw/api/v2/rest/dataset/{datasetId}";
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var stream = await resp.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
    }
}
