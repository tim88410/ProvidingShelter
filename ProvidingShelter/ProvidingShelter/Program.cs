using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Common.AppSettings;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Domain.SeedWork;
using ProvidingShelter.Infrastructure;
using ProvidingShelter.Infrastructure.Abstractions;
using ProvidingShelter.Infrastructure.Persistence;
using ProvidingShelter.Infrastructure.Repositories;
using ProvidingShelter.Infrastructure.Service;
using ProvidingShelter.Infrastructure.Service.DomainService;
using ProvidingShelter.Infrastructure.Service.ExternalService;
using ProvidingShelter.Web.Adapters.Providers;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// === DB ===
var cs = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(cs))
{
    builder.Services.AddDbContext<ShelterDbContext>(opt =>
        opt.UseSqlite("Data Source=providingshelter.db"));
}
else
{
    builder.Services.AddDbContext<ShelterDbContext>(opt =>
        opt.UseSqlServer(cs, sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 6,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: new[] { 233, -2, 4060, 40197, 40501, 40613 }
            );
            sql.CommandTimeout(120);
        }));
}

// 綁定 DataImportSettings（讓 IOptions<DataImportSettings> 可被注入）
builder.Services.Configure<DataImportSettings>(
    builder.Configuration.GetSection("DataImportSettings"));

builder.Services.AddOptions<DataImportSettings.SexualAssaultSettings>()
    .BindConfiguration("DataImportSettings:SexualAssault");

// DI
builder.Services.AddScoped<ISexualAssaultInformationRepository, SexualAssaultInformationRepository>();
builder.Services.AddHttpClient<OdsDownloader>();

builder.Services.AddScoped<ProvidingShelter.Application.Services.ISexualAssaultImportOrchestrator,
                           ProvidingShelter.Application.Services.SexualAssaultImportOrchestrator>();

builder.Services.AddSingleton<ILibreOfficeOptions, LibreOfficeOptionsProvider>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// JSON Enum（可選，讓前端用字串 enum）
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// CORS
const string FrontendPolicy = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// MemoryCache + Cache Options
builder.Services.AddMemoryCache();
builder.Services.Configure<ProvidingShelter.Common.AppSettings.AnalyticsCacheOptions>(
    builder.Configuration.GetSection("AnalyticsCache"));
builder.Services.AddSingleton<ProvidingShelter.Application.Services.Cache.ICacheProvider,
                              ProvidingShelter.Application.Services.Cache.MemoryCacheProvider>();

builder.Services.AddScoped<ISexualAssaultImportRepository, SexualAssaultImportRepository>();

builder.Services.AddScoped<ISexualAssaultNationalStatisticsRepository, SexualAssaultNationalStatisticsRepository>();
builder.Services.AddScoped<ProvidingShelter.Domain.Repositories.ISexualAssaultAnalyticsQueries,
                           ProvidingShelter.Infrastructure.Repositories.SexualAssaultAnalyticsQueryRepository>();


builder.Services.AddScoped<ICityCodeResolver, CityCodeResolver>();

builder.Services.AddInfrastructure();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(ProvidingShelter.Application.Queries.Analytics.Meta.GetAnalyticsMetaQuery).Assembly,
        typeof(ProvidingShelter.Application.Queries.Analytics.Panels.GetPanelsQuery).Assembly
    );
});
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(FrontendPolicy);
app.UseAuthorization();

app.MapControllers();

app.Run();
