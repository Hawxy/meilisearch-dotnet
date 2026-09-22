using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Meilisearch.Json
{
    /// <summary>
    /// Serializer options used by a client. Both variants share the caller's type info resolver, converters and
    /// the SDK's own metadata; they differ only in whether null members are written.
    /// </summary>
    internal sealed class MeilisearchJson
    {
        internal const string ReflectionMessage =
            "This constructor serializes document types with reflection-based System.Text.Json. " +
            "For trimmed or Native AOT applications, pass a JsonSerializerOptions whose TypeInfoResolver is a " +
            "JsonSerializerContext that registers your document types.";

        private MeilisearchJson(IJsonTypeInfoResolver resolver, JsonSerializerOptions template)
        {
            RemoveNulls = CreateOptions(resolver, template, JsonIgnoreCondition.WhenWritingNull);
            WriteNulls = CreateOptions(resolver, template, JsonIgnoreCondition.Never);
        }

        /// <summary>
        /// Options that omit null members. Used for document updates and settings writes.
        /// </summary>
        public JsonSerializerOptions RemoveNulls { get; }

        /// <summary>
        /// Options that write null members. Used for reads and for bodies where null carries meaning.
        /// </summary>
        public JsonSerializerOptions WriteNulls { get; }

        /// <summary>
        /// SDK metadata only. Resolves every SDK type and nothing else.
        /// </summary>
        public static MeilisearchJson Default { get; } = new MeilisearchJson(MeilisearchJsonContext.Default, null);

        /// <summary>
        /// Combines the caller's options with the SDK metadata. The caller's resolver must be set.
        /// </summary>
        public static MeilisearchJson Create(JsonSerializerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.TypeInfoResolver == null)
            {
                throw new ArgumentException(
                    "TypeInfoResolver must be set, for example to a JsonSerializerContext that registers your document types.",
                    nameof(options));
            }

            return new MeilisearchJson(JsonTypeInfoResolver.Combine(MeilisearchJsonContext.Default, options.TypeInfoResolver), options);
        }

        /// <summary>
        /// SDK metadata first, reflection for everything else.
        /// </summary>
        [RequiresUnreferencedCode(ReflectionMessage)]
        [RequiresDynamicCode(ReflectionMessage)]
        public static MeilisearchJson CreateReflectionDefault()
            => new MeilisearchJson(JsonTypeInfoResolver.Combine(MeilisearchJsonContext.Default, new DefaultJsonTypeInfoResolver()), null);

        /// <summary>
        /// Type info for reading, or for writing with nulls kept.
        /// </summary>
        public JsonTypeInfo<T> Info<T>() => TypeInfo<T>(WriteNulls);

        /// <summary>
        /// Type info for writing with nulls omitted.
        /// </summary>
        public JsonTypeInfo<T> InfoRemoveNulls<T>() => TypeInfo<T>(RemoveNulls);

        /// <summary>
        /// Resolves <typeparamref name="T"/> through <paramref name="options"/>, with an actionable error when it is unknown.
        /// </summary>
        public static JsonTypeInfo<T> TypeInfo<T>(JsonSerializerOptions options)
        {
            try
            {
                return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
            }
            catch (NotSupportedException e)
            {
                throw new NotSupportedException(
                    $"No JSON metadata is available for '{typeof(T)}'. Register it in a JsonSerializerContext and pass " +
                    "that context as JsonSerializerOptions.TypeInfoResolver when constructing MeilisearchClient.", e);
            }
        }

        private static JsonSerializerOptions CreateOptions(IJsonTypeInfoResolver resolver, JsonSerializerOptions template, JsonIgnoreCondition ignoreCondition)
        {
            var options = template == null
                ? new JsonSerializerOptions(JsonSerializerDefaults.Web)
                : new JsonSerializerOptions(template);

            // Meilisearch documents and SDK types are camelCase on the wire regardless of caller preferences.
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.AllowOutOfOrderMetadataProperties = true;
            options.DefaultIgnoreCondition = ignoreCondition;
            options.TypeInfoResolver = resolver;
            return options;
        }
    }
}
