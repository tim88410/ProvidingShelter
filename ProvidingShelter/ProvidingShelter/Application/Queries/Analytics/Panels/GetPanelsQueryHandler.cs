using MediatR;
using ProvidingShelter.Application.Services.Cache;
using ProvidingShelter.Common;
using ProvidingShelter.Domain.DTOs;
using ProvidingShelter.Domain.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProvidingShelter.Application.Queries.Analytics.Panels
{
    public sealed class GetPanelsQueryHandler : IRequestHandler<GetPanelsQuery, ApiResult.Result>
    {
        private readonly ISexualAssaultAnalyticsQueries _queries;
        private readonly ICacheProvider _cache;

        public GetPanelsQueryHandler(ISexualAssaultAnalyticsQueries queries, ICacheProvider cache)
        {
            _queries = queries;
            _cache = cache;
        }

        public async Task<ApiResult.Result> Handle(GetPanelsQuery request, CancellationToken ct)
        {
            // 以 Body 內容做快取 key
            var body = request.Body ?? new PanelRequestDto();
            var key = $"panels:city:{Hash(JsonSerializer.Serialize(body))}";

            //var data = await _cache.GetOrSetAsync(key,
            //    async _ => await _queries.GetSeriesPanelsAsync(body, ct),
            //    absoluteExpiration: TimeSpan.FromDays(3), // 也可改從設定來
            //    ct: ct);
            var data = await _queries.GetSeriesPanelsAsync(body, ct);

            return new ApiResult.Result
            {
                ReturnCode = ErrorCode.ReturnCode.OperationSuccessful,
                ReturnData = data
            };
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
