using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace ProvidingShelter.Importer.Pipeline
{
    public class CsvConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public CsvConverter(IJsonUtil json)
        {
            _json = json;
        }

        public string Name => "CSV";
        public bool CanHandle(ResourceContext ctx) => ctx.Format is "CSV" or "TXT";

        public async Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            using var reader = new StreamReader(ctx.LocalPath, detectEncodingFromByteOrderMarks: true);
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null,
                DetectDelimiter = true // CsvHelper 30+ 支援自動偵測
            };
            using var csv = new CsvReader(reader, cfg);

            var rows = new List<Dictionary<string, string?>>(capacity: 1000);
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();
            int count = 0;
            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var h in headers) dict[h] = csv.GetField(h);
                rows.Add(dict);
                if (++count >= 1000) break; // 預覽最多 1000 筆
            }

            return JsonSerializer.Serialize(rows, _json.Unescaped);
        }
    }
}
