using ProvidingShelter.Infrastructure.Abstractions;

namespace ProvidingShelter.Web.Adapters.Providers
{
    public sealed class LibreOfficeOptionsProvider : ILibreOfficeOptions
    {
        private readonly IConfiguration _cfg;
        public LibreOfficeOptionsProvider(IConfiguration cfg) => _cfg = cfg;

        public string? SofficePath =>
            _cfg["LibreOffice:SofficePath"]
            ?? Environment.GetEnvironmentVariable("LIBREOFFICE_PATH");
    }
}
