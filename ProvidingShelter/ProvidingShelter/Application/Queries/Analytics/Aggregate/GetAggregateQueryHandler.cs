using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;
using ProvidingShelter.Domain.Repositories;

namespace ProvidingShelter.Application.Queries.Analytics.Aggregate
{
    public sealed class GetAggregateQueryHandler
        : IRequestHandler<GetAggregateQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;

        public GetAggregateQueryHandler(ISexualAssaultAnalyticsQueries queries)
        {
            _queries = queries;
        }

        public async Task<ApiResult.Result> Handle(GetAggregateQuery request, CancellationToken ct)
        {
            var body = request.Body;

            if (body.Output == "heatmap" && body.View.Pivot != null)
            {
                var result = await _queries.GetHeatmapAsync(new HeatmapRequestDto
                {
                    Filters = body.Filters,
                    Pivot = body.View.Pivot!,
                    Metric = body.Metric,
                    Limit = body.Limit
                }, ct);

                return new ApiResult.Result { ReturnCode = ErrorCode.ReturnCode.OperationSuccessful, ReturnData = result };
            }

            // series or table
            var series = await _queries.GetSeriesAsync(body, ct);
            return new ApiResult.Result { ReturnCode = ErrorCode.ReturnCode.OperationSuccessful, ReturnData = series };
        }
    }
}
