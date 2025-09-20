using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProvidingShelter.Importer;
using ProvidingShelter.Importer.Pipeline;
using ProvidingShelter.Infrastructure.Persistence;
using System.Net;

var builder = Host.CreateApplicationBuilder(args);

// 設定
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args); // 支援 --mode=1/2/3/4、--delta=true、--keyword=xxx

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not configured.");

builder.Services.AddDbContext<ShelterDbContext>(opt =>
    opt.UseSqlServer(cs, sql =>
    {
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    })
);

// HttpClient
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

builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection("Storage"))
    .ValidateDataAnnotations();

builder.Services.AddOptions<FormatOptions>()
    .Bind(builder.Configuration.GetSection("Formats"))
    .ValidateDataAnnotations();

builder.Services.AddScoped<V2DatasetClient>();
builder.Services.AddScoped<ResourceRegistry>();
builder.Services.AddScoped<ResourceHarvester>();
builder.Services.AddScoped<ImporterRunner>();
builder.Services.AddScoped<IJsonUtil, JsonUtil>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<ImporterRunner>();

// 參數：--mode=1/2/3/4（舊參數 --delta=true 仍相容：等同 mode=2）
var modeArg = args.FirstOrDefault(a => a.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase));
int mode = 1;
if (modeArg != null && int.TryParse(modeArg.Split('=', 2)[1], out var m)) mode = m;
if (args.Any(a => string.Equals(a, "--delta=true", StringComparison.OrdinalIgnoreCase))) mode = 2;

// 只有 mode=4 會用到 --keyword=xxx
var keywordArg = args.FirstOrDefault(a => a.StartsWith("--keyword=", StringComparison.OrdinalIgnoreCase));
string? keyword = keywordArg?.Split('=', 2)[1];

await runner.RunAsync(mode, keyword, CancellationToken.None);
