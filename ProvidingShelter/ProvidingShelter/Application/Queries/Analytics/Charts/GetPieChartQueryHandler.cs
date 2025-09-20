using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.Repositories;

namespace ProvidingShelter.Application.Queries.Analytics.Charts
{
    public sealed class GetPieChartQueryHandler
        : IRequestHandler<GetPieChartQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;

        public GetPieChartQueryHandler(ISexualAssaultAnalyticsQueries queries) => _queries = queries;

        public async Task<ApiResult.Result> Handle(GetPieChartQuery request, CancellationToken ct)
        {
            var result = await _queries.GetPieAsync(request.Body, ct);
            return new ApiResult.Result { ReturnCode = ErrorCode.ReturnCode.OperationSuccessful, ReturnData = result };
        }
    }
}
