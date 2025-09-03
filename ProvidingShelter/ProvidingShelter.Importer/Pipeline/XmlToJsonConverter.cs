using System.Text.Json;
using System.Xml.Linq;

namespace ProvidingShelter.Importer.Pipeline
{
    public class XmlToJsonConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public XmlToJsonConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "XML";
        public bool CanHandle(ResourceContext ctx)
            => ctx.Format is "XML" || (ctx.ContentType?.Contains("xml", StringComparison.OrdinalIgnoreCase) ?? false);

        public Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            var x = XDocument.Load(ctx.LocalPath);
            var obj = XmlToDictionary(x.Root!);
            return Task.FromResult<string?>(JsonSerializer.Serialize(obj, _json.Unescaped));

            static object XmlToDictionary(XElement e)
            {
                // 簡單化：屬性 + 子節點；同名子節點→陣列
                var dict = new Dictionary<string, object?>();
                foreach (var a in e.Attributes()) dict[$"@{a.Name.LocalName}"] = a.Value;

                var groups = e.Elements().GroupBy(x => x.Name.LocalName);
                foreach (var g in groups)
                {
                    if (g.Count() == 1)
                        dict[g.Key] = XmlToDictionaryOrValue(g.First());
                    else
                        dict[g.Key] = g.Select(XmlToDictionaryOrValue).ToList();
                }

                if (!e.HasElements)
                {
                    var val = (e.Value ?? "").Trim();
                    if (!string.IsNullOrEmpty(val)) dict["#text"] = val;
                }
                return dict;
            }
            static object? XmlToDictionaryOrValue(XElement e)
            {
                return e.HasElements || e.Attributes().Any()
                    ? XmlToDictionary(e)
                    : (object?)(e.Value);
            }
        }
    }
}
