using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.Repositories;

namespace ProvidingShelter.Application.Queries.Analytics.GeoScatter
{
    public sealed class GetChoroplethQueryHandler
        : IRequestHandler<GetChoroplethQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;

        public GetChoroplethQueryHandler(ISexualAssaultAnalyticsQueries queries) => _queries = queries;

        public async Task<ApiResult.Result> Handle(GetChoroplethQuery request, CancellationToken ct)
        {
            var result = await _queries.GetChoroplethAsync(request.Body, ct);
            return new ApiResult.Result { ReturnCode = ErrorCode.ReturnCode.OperationSuccessful, ReturnData = result };
        }
    }
}
