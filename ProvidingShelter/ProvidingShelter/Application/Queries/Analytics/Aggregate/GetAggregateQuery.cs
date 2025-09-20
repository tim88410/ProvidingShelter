using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Application.Queries.Analytics.Aggregate
{
    public sealed class GetAggregateQuery : IRequest<ApiResult.Result>
    {
        public AggregateRequestDto Body { get; set; } = new();
    }
}
