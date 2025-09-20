using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.Repositories;

namespace ProvidingShelter.Application.Queries.Analytics.GeoScatter
{
    public sealed class GetScatterQueryHandler
        : IRequestHandler<GetScatterQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;

        public GetScatterQueryHandler(ISexualAssaultAnalyticsQueries queries) => _queries = queries;

        public async Task<ApiResult.Result> Handle(GetScatterQuery request, CancellationToken ct)
        {
            var result = await _queries.GetScatterAsync(request.Body, ct);
            return new ApiResult.Result { ReturnCode = ErrorCode.ReturnCode.OperationSuccessful, ReturnData = result };
        }
    }
}
