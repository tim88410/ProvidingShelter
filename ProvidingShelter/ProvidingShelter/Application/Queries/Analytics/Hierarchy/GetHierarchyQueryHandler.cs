using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.Repositories;

namespace ProvidingShelter.Application.Queries.Analytics.Hierarchy
{
    public sealed class GetHierarchyQueryHandler
        : IRequestHandler<GetHierarchyQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;

        public GetHierarchyQueryHandler(ISexualAssaultAnalyticsQueries queries) => _queries = queries;

        public async Task<ApiResult.Result> Handle(GetHierarchyQuery request, System.Threading.CancellationToken ct)
        {
            var result = await _queries.GetHierarchyAsync(request.Body, ct);
            return new ApiResult.Result { ReturnCode = ErrorCode.ReturnCode.OperationSuccessful, ReturnData = result };
        }
    }
}
