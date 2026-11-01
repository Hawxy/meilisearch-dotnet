using System.Text.Json.Serialization;

namespace Meilisearch.Json
{
    /// <summary>
    /// Body of the PATCH indexes/{uid} request.
    /// </summary>
    internal sealed class IndexPrimaryKeyPatch
    {
        [JsonPropertyName("primaryKey")]
        public string PrimaryKey { get; set; }
    }
}
