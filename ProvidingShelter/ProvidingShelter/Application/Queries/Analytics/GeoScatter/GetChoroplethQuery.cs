using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Application.Queries.Analytics.GeoScatter
{
    public sealed class GetChoroplethQuery : IRequest<ApiResult.Result>
    {
        public ChoroplethRequestDto Body { get; set; } = new();
    }
}
