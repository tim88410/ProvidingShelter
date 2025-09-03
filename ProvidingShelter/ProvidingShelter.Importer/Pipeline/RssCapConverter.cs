using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Xml;

namespace ProvidingShelter.Importer.Pipeline
{
    public class RssCapConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public RssCapConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "RSS/CAP";
        public bool CanHandle(ResourceContext ctx)
            => ctx.Format is "RSS" or "CAP";

        public Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            using var xr = XmlReader.Create(ctx.LocalPath);
            var feed = SyndicationFeed.Load(xr);
            var items = feed?.Items?.Select(i => new
            {
                i.Title?.Text,
                Summary = i.Summary?.Text,
                i.PublishDate,
                Links = i.Links.Select(l => l.Uri?.ToString()).ToArray()
            }).ToList();
            return Task.FromResult<string?>(
                JsonSerializer.Serialize(items, _json.Unescaped ?? new())
            );
        }
    }
}
