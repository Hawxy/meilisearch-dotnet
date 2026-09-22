using System.Text.Json.Serialization;

using Meilisearch.Converters;

namespace Meilisearch.QueryParameters
{
    public class DeleteDocumentsQuery
    {
        [JsonPropertyName("filter")]
        [JsonConverter(typeof(UntypedJsonConverter))]
        public object Filter { get; set; }
    }
}
