using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;

namespace ProvidingShelter.Application.Queries.Analytics.Hierarchy
{
    public sealed class GetHierarchyQuery : IRequest<ApiResult.Result>
    {
        public HierarchyRequestDto Body { get; set; } = new();
    }
}
