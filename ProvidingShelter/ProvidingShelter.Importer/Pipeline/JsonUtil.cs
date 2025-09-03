using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using Microsoft.Extensions.DependencyInjection;

namespace ProvidingShelter.Importer.Pipeline
{
    public interface IJsonUtil
    {
        /// <summary>
        /// 常用：不逃逸中日韓/拉丁等字元的 Json 選項（適合寫入 DB 便於人工閱讀）
        /// </summary>
        JsonSerializerOptions Unescaped { get; }

        /// <summary>
        /// 更鬆：UnsafeRelaxedJsonEscaping（包含 <, >, & 等也不會被逃逸）
        /// 嵌入 HTML 時請額外注意 XSS／做 HTML Encode。
        /// </summary>
        JsonSerializerOptions UnsafeRelaxed { get; }

        string SerializeUnescaped<T>(T value);
        string ToUnescapedJson(JsonNode node);
    }

    public sealed class JsonUtil : IJsonUtil
    {
        public JsonSerializerOptions Unescaped { get; }
        public JsonSerializerOptions UnsafeRelaxed { get; }

        public JsonUtil()
        {
            // 方法 A：明確允許常見東亞字元區段
            Unescaped = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.CjkUnifiedIdeographs,
                    UnicodeRanges.CjkSymbolsandPunctuation,
                    UnicodeRanges.HangulSyllables,
                    UnicodeRanges.Hiragana,
                    UnicodeRanges.Katakana
                ),
                WriteIndented = false
            };

            // 方法 B：更鬆（請注意安全情境）
            UnsafeRelaxed = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
        }

        public string SerializeUnescaped<T>(T value) => JsonSerializer.Serialize(value, Unescaped);

        public string ToUnescapedJson(JsonNode node) => node.ToJsonString(Unescaped);
    }

    /// <summary>
    /// 一鍵註冊 Importer Pipeline 所需服務（JsonUtil / ResourceRegistry / ResourceHarvester）
    /// </summary>
    public static class ImporterPipelineServiceCollectionExtensions
    {
        public static IServiceCollection AddImporterPipeline(this IServiceCollection services)
        {
            services.AddSingleton<IJsonUtil, JsonUtil>();
            services.AddSingleton<ResourceRegistry>(); // Registry 本身是無狀態，Singleton OK
            services.AddScoped<ResourceHarvester>();   // 內部使用 DbContext，採 Scoped
            return services;
        }
    }
}
