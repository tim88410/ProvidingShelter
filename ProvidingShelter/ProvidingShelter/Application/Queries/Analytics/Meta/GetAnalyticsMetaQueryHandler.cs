using MediatR;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.Repositories;

namespace ProvidingShelter.Application.Queries.Analytics.Meta
{
    public sealed class GetAnalyticsMetaQueryHandler
        : IRequestHandler<GetAnalyticsMetaQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;

        public GetAnalyticsMetaQueryHandler(ISexualAssaultAnalyticsQueries queries)
        {
            _queries = queries;
        }

        public async Task<ApiResult.Result> Handle(GetAnalyticsMetaQuery request, CancellationToken ct)
        {
            var meta = await _queries.GetDimensionsMetaAsync(ct);
            return new ApiResult.Result
            {
                ReturnCode = ErrorCode.ReturnCode.OperationSuccessful,
                ReturnData = meta
            };
        }
    }
}
