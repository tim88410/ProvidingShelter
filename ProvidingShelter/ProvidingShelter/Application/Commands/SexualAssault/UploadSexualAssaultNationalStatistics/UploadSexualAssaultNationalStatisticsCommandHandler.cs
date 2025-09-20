using MediatR;
using ProvidingShelter.Application.Services;
using ProvidingShelter.Common;

namespace ProvidingShelter.Application.Commands.SexualAssault.UploadSexualAssaultNationalStatistics
{
    public class UploadSexualAssaultNationalStatisticsCommandHandler
        : IRequestHandler<UploadSexualAssaultNationalStatisticsCommand, ErrorCode.ReturnCode>
    {
        private readonly ISexualAssaultImportOrchestrator _orchestrator;
        private readonly ILogger<UploadSexualAssaultNationalStatisticsCommandHandler> _logger;

        public UploadSexualAssaultNationalStatisticsCommandHandler(
            ISexualAssaultImportOrchestrator orchestrator,
            ILogger<UploadSexualAssaultNationalStatisticsCommandHandler> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        public async Task<ErrorCode.ReturnCode> Handle(
            UploadSexualAssaultNationalStatisticsCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var importId = await _orchestrator.UploadAndImportAgeByNationalityAsync(request.File, cancellationToken);
                return ErrorCode.ReturnCode.OperationSuccessful;
            }
            catch (Exception ex)
            {
                return ErrorCode.ReturnCode.OperationFailed;
            }
        }
    }
}
