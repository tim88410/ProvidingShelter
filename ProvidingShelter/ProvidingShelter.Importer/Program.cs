using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProvidingShelter.Importer;
using ProvidingShelter.Infrastructure.Persistence;
using System.Net;
using System.IO.Compression;

var builder = Host.CreateApplicationBuilder(args);

// 設定（支援環境覆寫，例如 DOTNET_ENVIRONMENT=Migrations 時會載入 appsettings.Migrations.json）
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? builder.Environment.EnvironmentName)}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured.");

builder.Services.AddDbContext<ShelterDbContext>(opt =>
    opt.UseSqlServer(cs, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 6,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: new[] { 233, -2, 4060, 40197, 40501, 40613 }
        );
        sql.CommandTimeout(300);
    }));

// HttpClient for OpenData（自動解壓）
builder.Services.AddHttpClient("opendata", c =>
{
    c.Timeout = TimeSpan.FromMinutes(10);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("ProvidingShelterImporter/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
});

// DI
builder.Services.AddSingleton<JsonArrayAsyncReader>();
builder.Services.AddScoped<DatasetImporter>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var importer = scope.ServiceProvider.GetRequiredService<DatasetImporter>();

// 參數：--delta=true 可切換異動清單
bool useDelta = args.Any(a => a.Equals("--delta=true", StringComparison.OrdinalIgnoreCase));
await importer.RunAsync(useDelta, CancellationToken.None);
