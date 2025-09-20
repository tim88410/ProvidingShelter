using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Application.Queries.Analytics.Charts
{
    public sealed class GetPieChartQuery : IRequest<ApiResult.Result>
    {
        public PieRequestDto Body { get; set; } = new();
    }
}
