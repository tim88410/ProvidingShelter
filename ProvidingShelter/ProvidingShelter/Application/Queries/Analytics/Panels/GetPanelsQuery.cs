using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Application.Queries.Analytics.Panels
{
    public sealed class GetPanelsQuery : IRequest<ApiResult.Result>
    {
        public PanelRequestDto Body { get; set; } = new();
    }
}
