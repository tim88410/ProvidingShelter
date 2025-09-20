using MediatR;
using ProvidingShelter.Common;

namespace ProvidingShelter.Application.Commands.Import
{
    public class ImportSexualAssaultCommand : IRequest<ApiResult.Result>
    {
        public string? cmd { get; set; }
    }
}
