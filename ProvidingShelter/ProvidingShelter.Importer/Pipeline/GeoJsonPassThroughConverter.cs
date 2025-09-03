namespace ProvidingShelter.Importer.Pipeline
{
    public class GeoJsonPassThroughConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public GeoJsonPassThroughConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "GEOJSON";
        public bool CanHandle(ResourceContext ctx) => ctx.Format is "GEOJSON";

        public async Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            return await File.ReadAllTextAsync(ctx.LocalPath, ct);
        }
    }
}
