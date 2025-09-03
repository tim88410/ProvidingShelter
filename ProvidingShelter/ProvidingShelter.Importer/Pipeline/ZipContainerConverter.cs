using System.IO.Compression;

namespace ProvidingShelter.Importer.Pipeline
{
    public class ZipContainerConverter : IResourceConverter
    {
        private readonly IJsonUtil _json;

        public ZipContainerConverter(IJsonUtil json)
        {
            _json = json;
        }
        public string Name => "ZIP (extract-only)";
        public bool CanHandle(ResourceContext ctx) => ctx.Format is "ZIP";

        public Task<string?> ConvertToJsonAsync(ResourceContext ctx, CancellationToken ct)
        {
            var dir = Path.Combine(Path.GetDirectoryName(ctx.LocalPath)!, "extracted");
            Directory.CreateDirectory(dir);
            ZipFile.ExtractToDirectory(ctx.LocalPath, dir, overwriteFiles: true);
            return Task.FromResult<string?>(null);
        }
    }
}
