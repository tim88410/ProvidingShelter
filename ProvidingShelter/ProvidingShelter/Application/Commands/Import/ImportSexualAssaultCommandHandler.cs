
using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Infrastructure.Repositories;
using ProvidingShelter.Infrastructure.Service.DomainService;

namespace ProvidingShelter.Application.Commands.Import
{
    public class ImportSexualAssaultCommandHandler : IRequestHandler<ImportSexualAssaultCommand, ApiResult.Result>
    {
        private readonly IDatasetResourceQueries _queries;
        private readonly ISexualAssaultImportService _importService;

        public ImportSexualAssaultCommandHandler(
            IDatasetResourceQueries queries,
            ISexualAssaultImportService importService)
        {
            _queries = queries;
            _importService = importService;
        }

        public async Task<ApiResult.Result> Handle(ImportSexualAssaultCommand command, CancellationToken ct = default)
        {
            if (!string.Equals(command.cmd, "importSexualAssault", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResult.Result
                {
                    ReturnCode = ErrorCode.ReturnCode.OperationFailed
                };
            }

            var urls = await _queries.GetSexualAssaultOdsDetailAsync(ct);
            if (urls.Count == 0)
            {
                return new ApiResult.Result
                {
                    ReturnCode = ErrorCode.ReturnCode.DataNotFound,
                    ReturnData = "No ODS found to import."
                };
            }

            var count = await _importService.ImportFromOdsUrlsAsync(urls, ct);
            return new ApiResult.Result
            {
                ReturnCode = ErrorCode.ReturnCode.OperationSuccessful,
                ReturnData = $"Imported {count}"
            };
        }
    }
}
