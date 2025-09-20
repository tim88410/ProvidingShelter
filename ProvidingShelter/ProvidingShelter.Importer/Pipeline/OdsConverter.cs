using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;

namespace ProvidingShelter.Importer.Pipeline
{
    public class OdsConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public OdsConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "ODS";
        public bool CanHandle(ResourceContext ctx) => ctx.Format is "ODS";

        public async Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            using var zip = ZipFile.OpenRead(ctx.LocalPath);
            var entry = zip.GetEntry("content.xml");
            if (entry == null) return null;

            using var s = entry.Open();
            var xdoc = XDocument.Load(s);

            XNamespace tableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
            XNamespace textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

            var table = xdoc.Descendants(tableNs + "table").FirstOrDefault();
            if (table == null) return null;

            // 取第一列當標題
            var rows = table.Elements(tableNs + "table-row").ToList();
            if (!rows.Any()) return "[]";

            var headers = GetRowValues(rows[0], tableNs, textNs);
            var data = new List<Dictionary<string, string?>>();
            for (int i = 1; i < rows.Count && i <= 100000; i++)
            {
                ct.ThrowIfCancellationRequested();
                var vals = GetRowValues(rows[i], tableNs, textNs);
                var dict = new Dictionary<string, string?>();
                for (int c = 0; c < headers.Count; c++)
                {
                    var key = string.IsNullOrWhiteSpace(headers[c]) ? $"col{c + 1}" : headers[c]!;
                    dict[key] = c < vals.Count ? vals[c] : null;
                }
                data.Add(dict);
            }
            return JsonSerializer.Serialize(data, _json.Unescaped);

            static List<string?> GetRowValues(XElement row, XNamespace tableNs, XNamespace textNs)
            {
                var cells = row.Elements(tableNs + "table-cell").ToList();
                var vals = new List<string?>();
                foreach (var cell in cells)
                {
                    var p = cell.Element(textNs + "p");
                    vals.Add(p?.Value);
                }
                return vals;
            }
        }
    }
}
