using System.Text;
using System.Text.Json;

namespace ProvidingShelter.Importer.Pipeline
{
    public class TxtSniffConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public TxtSniffConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "TXT (sniff)";
        public bool CanHandle(ResourceContext ctx) => ctx.Format is "TXT";

        public async Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            var lines = await File.ReadAllLinesAsync(ctx.LocalPath, Encoding.UTF8, ct);
            if (lines.Length == 0) return "[]";
            var delims = new[] { '\t', ',', ';', '|' };
            var delim = delims.OrderByDescending(d => lines[0].Count(c => c == d)).First();

            var headers = lines[0].Split(delim);
            var list = new List<Dictionary<string, string?>>();
            for (int i = 1; i < Math.Min(lines.Length, 1001); i++)
            {
                ct.ThrowIfCancellationRequested();
                var vals = lines[i].Split(delim);
                var dict = new Dictionary<string, string?>();
                for (int c = 0; c < headers.Length; c++)
                {
                    var key = string.IsNullOrWhiteSpace(headers[c]) ? $"col{c + 1}" : headers[c];
                    dict[key] = c < vals.Length ? vals[c] : null;
                }
                list.Add(dict);
            }
            return JsonSerializer.Serialize(list, _json.Unescaped);
        }
    }
}
