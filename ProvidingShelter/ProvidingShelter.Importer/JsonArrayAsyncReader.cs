using System.Text.Json;

namespace ProvidingShelter.Importer
{
    public sealed class JsonArrayAsyncReader
    {
        private static string? ToStringValue(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                // 物件或陣列：保留原樣（給你需要時還原）
                JsonValueKind.Object or JsonValueKind.Array => el.GetRawText(),
                _ => el.GetRawText()
            };
        }

        /// <summary>
        /// 串流方式將 JSON 陣列逐筆轉成 Dictionary&lt;string,string?&gt;。
        /// </summary>
        public async IAsyncEnumerable<Dictionary<string, string?>> ReadAsync(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultBufferSize = 32 * 1024
            };

            await foreach (var obj in JsonSerializer
                .DeserializeAsyncEnumerable<Dictionary<string, JsonElement>>(
                    stream, options, ct))
            {
                if (obj is null) continue;

                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in obj)
                {
                    dict[kv.Key] = ToStringValue(kv.Value);
                }
                yield return dict;
            }
        }
    }
}
