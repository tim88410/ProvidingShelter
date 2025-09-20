using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Application.Queries.Analytics.GeoScatter
{
    public sealed class GetScatterQuery : IRequest<ApiResult.Result>
    {
        public ScatterRequestDto Body { get; set; } = new();
    }
}
