using Microsoft.EntityFrameworkCore;
using ProvidingShelter.Application.Commands.Crawler;
using ProvidingShelter.Application.Queries.Backend;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Infrastructure.Persistence;
using ProvidingShelter.Infrastructure.Repositories;
using ProvidingShelter.Infrastructure.Service.ExternalService;

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

// === DI ===
builder.Services.AddHttpClient<DataGovCrawler>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddScoped<CrawlDatasetsCommandHandler>();
builder.Services.AddScoped<GetDatasetResourcesQuery>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// --- Endpoints ---
app.MapPost("/api/crawl", async (
    CrawlDatasetsCommand cmd,
    CrawlDatasetsCommandHandler handler,
    CancellationToken ct) =>
{
    var count = await handler.HandleAsync(cmd, ct);
    return Results.Ok(new { upserted = count });
});

app.MapGet("/api/datasets/{dataId}/resources", async (
    string dataId,
    GetDatasetResourcesQuery query,
    CancellationToken ct) =>
{
    var list = await query.HandleAsync(dataId, ct);
    return Results.Ok(list);
});

app.Run();
