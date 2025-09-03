namespace ProvidingShelter.Importer.Pipeline
{
    public class JsonPassThroughConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public JsonPassThroughConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "JSON";
        public bool CanHandle(ResourceContext ctx)
            => ctx.Format is "JSON" || (ctx.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false);

        public async Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            return await File.ReadAllTextAsync(ctx.LocalPath, ct);
        }
    }
}
