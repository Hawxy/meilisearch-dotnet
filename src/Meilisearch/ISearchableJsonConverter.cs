using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using Meilisearch.Json;

namespace Meilisearch
{
    /// <summary>
    /// The json converter factory for <see cref="ISearchable{T}"/>. Creates <see cref="ISearchableJsonConverter{T}"/>
    /// for the requested hit type at runtime, so it is only usable with reflection-based serialization.
    /// The client does not use it; it serves consumers who serialize <see cref="ISearchable{T}"/> themselves.
    /// </summary>
    [RequiresDynamicCode(Message)]
    [RequiresUnreferencedCode(Message)]
    public class ISearchableJsonConverterFactory : JsonConverterFactory
    {
        private const string Message =
            "ISearchableJsonConverterFactory creates converters for arbitrary hit types at runtime. " +
            "Serialize SearchResult<T> or PaginatedSearchResult<T> directly with source-generated metadata instead.";

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsInterface
                && typeToConvert.IsGenericType
                && (typeToConvert.GetGenericTypeDefinition() == typeof(ISearchable<>));
        }

        /// <inheritdoc/>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var genericArgs = typeToConvert.GetGenericArguments();
            var converterType = typeof(ISearchableJsonConverter<>).MakeGenericType(
                genericArgs[0]
            );
            var converter = (JsonConverter)Activator.CreateInstance(converterType);
            return converter;
        }
    }

    /// <summary>
    /// The json converter for <see cref="ISearchable{T}"/>. Requires <see cref="SearchResult{T}"/> and
    /// <see cref="PaginatedSearchResult{T}"/> to be resolvable through the serializer options.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ISearchableJsonConverter<T> : JsonConverter<ISearchable<T>>
    {
        /// <inheritdoc/>
        public override ISearchable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var document = JsonElement.ParseValue(ref reader);
            return document.TryGetProperty("page", out _) || document.TryGetProperty("hitsPerPage", out _)
                ? document.Deserialize(MeilisearchJson.TypeInfo<PaginatedSearchResult<T>>(options))
                : (ISearchable<T>)document.Deserialize(MeilisearchJson.TypeInfo<SearchResult<T>>(options));
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ISearchable<T> value, JsonSerializerOptions options)
        {
            if (value is PaginatedSearchResult<T> paginated)
            {
                JsonSerializer.Serialize(writer, paginated, MeilisearchJson.TypeInfo<PaginatedSearchResult<T>>(options));
            }
            else if (value is SearchResult<T> normal)
            {
                JsonSerializer.Serialize(writer, normal, MeilisearchJson.TypeInfo<SearchResult<T>>(options));
            }
            else
            {
                JsonSerializer.Serialize(writer, value, options.GetTypeInfo(value.GetType()));
            }
        }
    }
}
