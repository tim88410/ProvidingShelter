using Microsoft.Extensions.DependencyInjection;
using ProvidingShelter.Infrastructure.Abstractions;
using ProvidingShelter.Infrastructure.Repositories;
using ProvidingShelter.Infrastructure.Service.DomainService;
using ProvidingShelter.Infrastructure.Service.ExternalService;

namespace ProvidingShelter.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IOdsDownloader, OdsDownloader>();
            services.AddScoped<IOdsReader, OdsReader>();
            services.AddScoped<ISexualAssaultImportService, SexualAssaultImportService>();
            services.AddScoped<IDatasetResourceQueries, DatasetResourceQueries>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<ISexualAssaultNationalityStatisticsParser, ClosedXmlSexualAssaultNationalityStatisticsParser>();
            services.AddSingleton<ILibreOfficeConvertService, LibreOfficeConvertService>();
            services.AddScoped<ProvidingShelter.Infrastructure.Service.DomainService.ICityCodeSyncService,
                           ProvidingShelter.Infrastructure.Service.DomainService.CityCodeSyncService>();
            return services;
        }
    }
}
