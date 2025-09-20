using Microsoft.Extensions.Logging;

namespace ProvidingShelter.Importer.Pipeline
{
    public record ResourceContext
    {
        public string DatasetId { get; init; } = default!;
        public string ResourceKey { get; init; } = default!;
        public string SourceUrl { get; init; } = default!;
        public string? ContentType { get; init; }
        public string Format { get; init; } = "";
        public string LocalPath { get; init; } = default!;
    }

    public interface IResourceConverter
    {
        bool CanHandle(ResourceContext ctx);
        Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct);
        string Name { get; }
    }

    public class ResourceRegistry
    {
        private readonly List<IResourceConverter> _converters;
        private readonly ILogger<ResourceRegistry> _logger;

        public ResourceRegistry(ILogger<ResourceRegistry> logger, IJsonUtil json)
        {
            _logger = logger;
            _converters = new()
            {
                new CsvConverter(json),
                new JsonPassThroughConverter(json),
                new XmlToJsonConverter(json),
                new ExcelConverter(json),
                new OdsConverter(json),
                new RssCapConverter(json),
                new TxtSniffConverter(json),
                new GeoJsonPassThroughConverter(json),
                new ZipContainerConverter(json), // 只解壓，不轉；內部留擴充點
            };
        }

        public async Task<(string converter, string? json)> TryConvertAsync(ResourceContext ctx, CancellationToken ct)
        {
            var conv = _converters.FirstOrDefault(c => c.CanHandle(ctx));
            if (conv == null)
            {
                _logger.LogInformation("No converter for {fmt} ({url})", ctx.Format, ctx.SourceUrl);
                return ("(none)", null);
            }
            try
            {
                var json = await conv.ConvertToJsonAsync(ctx, ct);
                return (conv.Name, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Converter {name} failed", conv.Name);
                return (conv.Name + " (failed)", null);
            }
        }
    }
}
