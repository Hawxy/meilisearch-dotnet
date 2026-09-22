using System.Text.Json;
using System.Text.Json.Serialization;

using Meilisearch.Tests.Models;

namespace Meilisearch.Tests
{
    /// <summary>
    /// Source-generated metadata for every document type the tests send or receive.
    /// </summary>
    [JsonSerializable(typeof(Movie))]
    [JsonSerializable(typeof(MovieStruct))]
    [JsonSerializable(typeof(MovieWithInfo))]
    [JsonSerializable(typeof(MovieWithIntId))]
    [JsonSerializable(typeof(FormattedMovie))]
    [JsonSerializable(typeof(MovieWithRankingScore))]
    [JsonSerializable(typeof(MovieWithRankingScoreDetails))]
    [JsonSerializable(typeof(KeyedMovie))]
    [JsonSerializable(typeof(Product))]
    [JsonSerializable(typeof(VectorMovie))]
    [JsonSerializable(typeof(DatasetSmallMovie))]
    [JsonSerializable(typeof(DatasetSong))]
    internal partial class TestJsonContext : JsonSerializerContext
    {
    }

    internal static class TestJson
    {
        /// <summary>
        /// Client options that resolve document types through <see cref="TestJsonContext"/> only.
        /// </summary>
        public static readonly JsonSerializerOptions SourceGen = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = TestJsonContext.Default };

        /// <summary>
        /// The SDK metadata a client built from <see cref="SourceGen"/> uses.
        /// </summary>
        public static readonly Meilisearch.Json.MeilisearchJson SourceGenJson = Meilisearch.Json.MeilisearchJson.Create(SourceGen);

        /// <summary>
        /// Reflection-based options matching the client's null-omitting output, for comparing arbitrary values.
        /// </summary>
        public static readonly JsonSerializerOptions ReflectionRemoveNulls = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowOutOfOrderMetadataProperties = true,
        };
    }
}
