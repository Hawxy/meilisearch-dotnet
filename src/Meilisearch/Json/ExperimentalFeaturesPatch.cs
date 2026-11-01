using System.Text.Json.Serialization;

namespace Meilisearch.Json
{
    /// <summary>
    /// Body of the PATCH experimental-features request.
    /// </summary>
    internal sealed class ExperimentalFeaturesPatch
    {
        [JsonPropertyName("dynamicSearchRules")]
        public bool DynamicSearchRules { get; set; }
    }
}
