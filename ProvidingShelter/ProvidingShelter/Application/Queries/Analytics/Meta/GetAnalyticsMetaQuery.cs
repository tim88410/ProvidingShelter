using MediatR;
using ProvidingShelter.Common;

namespace ProvidingShelter.Application.Queries.Analytics.Meta
{
    public sealed class GetAnalyticsMetaQuery : IRequest<ApiResult.Result> { }
}
