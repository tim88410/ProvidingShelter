using Microsoft.Extensions.Options;
using ProvidingShelter.Common.AppSettings;
using ProvidingShelter.Domain.Aggregates.SexualAssaultAggregate;
using ProvidingShelter.Domain.Repositories;
using ProvidingShelter.Domain.SeedWork;
using ProvidingShelter.Infrastructure.Abstractions;
using ProvidingShelter.Infrastructure.Service.DomainService;
using ProvidingShelter.Infrastructure.Service.ExternalService;

namespace ProvidingShelter.Application.Services
{
    public sealed class SexualAssaultImportOrchestrator : ISexualAssaultImportOrchestrator
    {
        private readonly IFileStorageService _fileStorage;
        private readonly ILibreOfficeConvertService _converter;
        private readonly ISexualAssaultNationalityStatisticsParser _parser;
        private readonly ISexualAssaultImportRepository _importRepo;
        private readonly ISexualAssaultNationalStatisticsRepository _statRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICityCodeSyncService _cityCodeSync;
        private readonly IOptions<DataImportSettings> _options;
        private readonly ILogger<SexualAssaultImportOrchestrator> _logger;

        public SexualAssaultImportOrchestrator(
            IFileStorageService fileStorage,
            ILibreOfficeConvertService converter,
            ISexualAssaultNationalityStatisticsParser parser,
            ISexualAssaultImportRepository importRepo,
            ISexualAssaultNationalStatisticsRepository statRepo,
            IUnitOfWork uow,
            ICityCodeSyncService cityCodeSync,
            IOptions<DataImportSettings> options,
            ILogger<SexualAssaultImportOrchestrator> logger)
        {
            _fileStorage = fileStorage;
            _converter = converter;
            _parser = parser;
            _importRepo = importRepo;
            _statRepo = statRepo;
            _uow = uow;
            _cityCodeSync = cityCodeSync;
            _options = options;
            _logger = logger;
        }

        public async Task<Guid> UploadAndImportAgeByNationalityAsync(IFormFile file, CancellationToken ct)
        {
            if (file is null || file.Length <= 0)
                throw new ArgumentException("上傳檔案為空。", nameof(file));

            var basePath = _options.Value.SexualAssault.BasePath;
            if (string.IsNullOrWhiteSpace(basePath))
                throw new InvalidOperationException("DataImportSettings.SexualAssault.BasePath 未設定。");

            // 1) 儲存檔案（含 SHA256）
            var saveResult = await _fileStorage.SaveAsync(file, basePath, ct);
            var storedFullPath = saveResult.storedFullPath;
            var fileHash = saveResult.fileHashSha256;

            // 2) 去重：若已匯入過，直接回傳既有 ImportId
            var existed = await _importRepo.FindByHashAsync(fileHash, ct);
            if (existed is not null)
            {
                _logger.LogInformation("檔案已匯入過，ImportId={ImportId}", existed.Id);
                return existed.Id;
            }

            // 3) 轉檔 ODS→XLSX
            var xlsxPath = await _converter.ConvertOdsToXlsxAsync(storedFullPath, ct);

            // 4) 解析 XLSX（國籍 × 行業）
            var parsed = await _parser.ParseAsync(xlsxPath, ct);

            // 5) 建立 Import 紀錄
            var import = new SexualAssaultImport(
                sourceFileName: file.FileName,
                storedFullPath: storedFullPath,
                fileHashSha256: fileHash,
                crossTableTitle: parsed.CrossTableTitle,
                categoryType: CrossCategoryType.Industry,
                periodYearStart: parsed.PeriodYearStart,
                periodYearEnd: parsed.PeriodYearEnd
            );
            import.SetCounters(parsed.RawRowCount, parsed.ParsedRowCount);

            await _importRepo.AddAsync(import, ct);

            // 6) 直接轉成 Entity（CityCode 先給 UNK，CityName 用原字串或做最輕微正規化）
            static string NormalizeCityName(string s) => (s ?? string.Empty).Trim().Replace("台", "臺"); // 視你的 RisCityCode 內容調整
            var stats = new List<SexualAssaultStat>(parsed.Items.Count);

            foreach (var item in parsed.Items)
            {
                var entity = new SexualAssaultStat(
                    importId: import.Id,
                    year: item.Year,
                    cityCode: "",                          // 暫用；稍後由 _cityCodeSync 依 CityName 更新
                    cityName: NormalizeCityName(item.CityName),
                    nationality: item.Nationality,
                    categoryType: item.CategoryType,             // 由 parser 決定（此處是 Industry）
                    categoryKey: item.CategoryKey,
                    categoryNameZh: item.CategoryNameZh,
                    count: item.Count,
                    isTotalRow: item.IsTotalRow
                );
                stats.Add(entity);
            }

            // 7) 寫入資料庫（可由 Infra 實作 BulkInsert）
            await _statRepo.BulkInsertAsync(stats, ct);

            // 8) 先提交一次（確保資料已入庫）
            await _uow.SaveChangesAsync(ct);

            // 9) 匯入後同步 CityCode：依 CityName 分組取數值最大 CityCode 為當前
            var (updatedRis, updatedStat) = await _cityCodeSync.SyncLatestByCityNameAsync(ct);
            _logger.LogInformation("CityCode 同步完成：RisCityCode 更新={UpdatedRis}，SexualAssaultStat 更新={UpdatedStat}", updatedRis, updatedStat);

            _logger.LogInformation("匯入完成：ImportId={ImportId}，Parsed={ParsedCount}", import.Id, parsed.ParsedRowCount);
            return import.Id;
        }
    }
}
