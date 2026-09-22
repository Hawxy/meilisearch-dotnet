using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Meilisearch.AotSmoke
{
    public class Movie
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Genre { get; set; }

        public int? Year { get; set; }
    }

    /// <summary>
    /// Metadata for the application's own document types. The SDK supplies metadata for its request and response types.
    /// </summary>
    [JsonSerializable(typeof(Movie))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
