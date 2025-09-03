using ExcelDataReader;
using System.Data;
using System.Text.Json;

namespace ProvidingShelter.Importer.Pipeline
{
    public class ExcelConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public ExcelConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "Excel (XLS/XLSX)";
        public bool CanHandle(ResourceContext ctx) => ctx.Format is "XLS" or "XLSX";

        public async Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var fs = File.OpenRead(ctx.LocalPath);
            using var reader = ExcelReaderFactory.CreateReader(fs);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            if (table == null) return "[]";

            var list = new List<Dictionary<string, object?>>();
            int count = 0;
            foreach (DataRow row in table.Rows)
            {
                ct.ThrowIfCancellationRequested();
                var dict = new Dictionary<string, object?>();
                foreach (DataColumn col in table.Columns)
                    dict[col.ColumnName] = row[col];
                list.Add(dict);
                if (++count >= 1000) break;
            }
            return JsonSerializer.Serialize(list, _json.Unescaped);
        }
    }
}
