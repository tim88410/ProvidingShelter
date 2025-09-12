using MediatR;
using Microsoft.AspNetCore.Http;
using ProvidingShelter.Common;

namespace ProvidingShelter.Application.Commands.SexualAssault.UploadSexualAssaultNationalStatistics
{
    public sealed record UploadSexualAssaultNationalStatisticsCommand(IFormFile File)
        : IRequest<ErrorCode.ReturnCode>;
}
