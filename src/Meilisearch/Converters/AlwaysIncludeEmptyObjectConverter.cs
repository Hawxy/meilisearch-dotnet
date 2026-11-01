using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Meilisearch.Converters
{
    /// <summary>
    /// Always include property in json. MultiSearchFederationOptions will be serialized as "{}"
    /// </summary>
    public class MultiSearchFederationOptionsConverter : JsonConverter<MultiSearchFederationOptions>
    {
        /// <inheritdoc/>
        public override MultiSearchFederationOptions Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(ref reader, TypeInfo(options));
        }

        /// <summary>
        /// Write json for MultiSearchFederationOptions and include it always as empty object
        /// </summary>
        public override void Write(Utf8JsonWriter writer, MultiSearchFederationOptions value,
            JsonSerializerOptions options)
        {
            if (value == null || (value.Offset == 0 && value.Limit == 0))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }

            JsonSerializer.Serialize(writer, value, TypeInfo(options));
        }

        private static JsonTypeInfo<MultiSearchFederationOptions> TypeInfo(JsonSerializerOptions options)
            => (JsonTypeInfo<MultiSearchFederationOptions>)options.GetTypeInfo(typeof(MultiSearchFederationOptions));
    }
}
