using System.IO.Compression;
using System.Xml.Linq;

namespace ProvidingShelter.Infrastructure.Service.ExternalService
{
    public interface IOdsReader
    {
        /// <summary>讀取 ODS，回傳每列資料（以「標頭 → 值」字典；標頭為原始大寫）。</summary>
        Task<List<Dictionary<string, string>>> ReadAsync(Stream odsStream, CancellationToken ct = default);
    }

    public class OdsReader : IOdsReader
    {
        public async Task<List<Dictionary<string, string>>> ReadAsync(Stream odsStream, CancellationToken ct = default)
        {
            using var zip = new ZipArchive(odsStream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = zip.GetEntry("content.xml") ?? throw new InvalidOperationException("ODS content.xml not found");
            using var es = entry.Open();
            using var ms = new MemoryStream();
            await es.CopyToAsync(ms, ct);
            ms.Position = 0;

            var xdoc = XDocument.Load(ms);

            XNamespace office = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
            XNamespace table = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
            XNamespace text = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

            var rows = new List<List<string>>();

            var sheet = xdoc
                .Descendants(office + "document-content")
                .Descendants(office + "body")
                .Descendants(office + "spreadsheet")
                .Descendants(table + "table")
                .FirstOrDefault();

            if (sheet == null) return new List<Dictionary<string, string>>();

            foreach (var row in sheet.Elements(table + "table-row"))
            {
                var cells = new List<string>();
                foreach (var cell in row.Elements(table + "table-cell"))
                {
                    var repeatAttr = cell.Attribute(table + "number-columns-repeated");
                    int repeat = repeatAttr != null && int.TryParse(repeatAttr.Value, out var r) ? r : 1;

                    string textVal = string.Join("\n",
                        cell.Elements(text + "p").Select(p => (p.Value ?? string.Empty).Trim()));

                    for (int i = 0; i < repeat; i++)
                        cells.Add(textVal);
                }
                if (cells.All(string.IsNullOrWhiteSpace)) continue;
                rows.Add(cells);
            }

            if (rows.Count == 0) return new List<Dictionary<string, string>>();

            var headers = rows[0].Select(h => (h ?? string.Empty).Trim().ToUpperInvariant()).ToList();
            var result = new List<Dictionary<string, string>>();

            foreach (var r in rows.Skip(1))
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count; i++)
                {
                    var key = headers[i];
                    var val = i < r.Count ? r[i] : string.Empty;
                    dict[key] = val?.Trim() ?? string.Empty;
                }
                if (dict.Values.All(string.IsNullOrWhiteSpace)) continue;
                result.Add(dict);
            }

            return result;
        }
    }
}
