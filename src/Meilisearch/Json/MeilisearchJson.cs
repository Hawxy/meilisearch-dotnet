using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

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

        private static readonly ConditionalWeakTable<JsonSerializerOptions, MeilisearchJson> s_byOptions = new ConditionalWeakTable<JsonSerializerOptions, MeilisearchJson>();
        private static MeilisearchJson s_reflectionDefault;

        /// <summary>
        /// Metadata built at runtime for types the resolver cannot supply, keyed by the options it was built for
        /// and the type it describes.
        /// </summary>
        private readonly ConcurrentDictionary<(JsonSerializerOptions, Type), JsonTypeInfo> _built = new ConcurrentDictionary<(JsonSerializerOptions, Type), JsonTypeInfo>();

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
        /// Combines the caller's options with the SDK metadata. The caller's resolver must be set. Clients built
        /// from the same options instance share one result, so contract caches are built once.
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

            return s_byOptions.GetValue(options, o => new MeilisearchJson(JsonTypeInfoResolver.Combine(MeilisearchJsonContext.Default, o.TypeInfoResolver), o));
        }

        /// <summary>
        /// SDK metadata first, reflection for everything else. Shared by every client that uses reflection, so
        /// the contract caches are built once per process.
        /// </summary>
        [RequiresUnreferencedCode(ReflectionMessage)]
        [RequiresDynamicCode(ReflectionMessage)]
        public static MeilisearchJson ReflectionDefault()
        {
            var existing = Volatile.Read(ref s_reflectionDefault);
            if (existing != null)
            {
                return existing;
            }

            var created = new MeilisearchJson(JsonTypeInfoResolver.Combine(MeilisearchJsonContext.Default, new DefaultJsonTypeInfoResolver()), null);
            return Interlocked.CompareExchange(ref s_reflectionDefault, created, null) ?? created;
        }

        /// <summary>
        /// Type info for reading, or for writing with nulls kept.
        /// </summary>
        public JsonTypeInfo<T> Info<T>() => TypeInfo<T>(WriteNulls);

        /// <summary>
        /// Type info for writing with nulls omitted.
        /// </summary>
        public JsonTypeInfo<T> InfoRemoveNulls<T>() => TypeInfo<T>(RemoveNulls);

        /// <summary>
        /// Type info for a list of <typeparamref name="T"/> whose elements the resolver supplies.
        /// </summary>
        public JsonTypeInfo<List<T>> ListInfo<T>() => ListInfo(Info<T>());

        /// <summary>
        /// Type info for a list of <typeparamref name="T"/> built from <paramref name="elementInfo"/>.
        /// </summary>
        public JsonTypeInfo<List<T>> ListInfo<T>(JsonTypeInfo<T> elementInfo)
            => Built(WriteNulls, elementInfo, static (options, element) =>
            {
                return JsonMetadataServices.CreateListInfo<List<T>, T>(options, new JsonCollectionInfoValues<List<T>>
                {
                    ObjectCreator = () => new List<T>(),
                    ElementInfo = element,
                });
            });

        /// <summary>
        /// Type info for writing a sequence of <typeparamref name="T"/> as one JSON array, nulls included.
        /// </summary>
        public JsonTypeInfo<IEnumerable<T>> ItemsInfo<T>() => ItemsInfo<T>(WriteNulls);

        /// <summary>
        /// Type info for writing a sequence of <typeparamref name="T"/> as one JSON array, nulls omitted.
        /// </summary>
        public JsonTypeInfo<IEnumerable<T>> ItemsInfoRemoveNulls<T>() => ItemsInfo<T>(RemoveNulls);

        private JsonTypeInfo<IEnumerable<T>> ItemsInfo<T>(JsonSerializerOptions options)
            => Built(options, this, static (o, _) => JsonMetadataServices.CreateIEnumerableInfo<IEnumerable<T>, T>(o, new JsonCollectionInfoValues<IEnumerable<T>>
            {
                ElementInfo = TypeInfo<T>(o),
            }));

        /// <summary>
        /// Contract for a search response with <typeparamref name="T"/> hits.
        /// </summary>
        public JsonTypeInfo<SearchEnvelope<T>> SearchEnvelopeInfo<T>()
            => Built(WriteNulls, this, static (_, json) => EnvelopeContracts.Search<T>(json));

        /// <summary>
        /// Contract for a paginated list response with <typeparamref name="T"/> results.
        /// </summary>
        public JsonTypeInfo<ResourceResultsEnvelope<T>> ResourceResultsEnvelopeInfo<T>()
            => Built(WriteNulls, this, static (_, json) => EnvelopeContracts.ResourceResults<T>(json));

        /// <summary>
        /// Contract for a similar-documents response with <typeparamref name="T"/> hits.
        /// </summary>
        public JsonTypeInfo<SimilarDocumentsEnvelope<T>> SimilarDocumentsEnvelopeInfo<T>()
            => Built(WriteNulls, this, static (_, json) => EnvelopeContracts.SimilarDocuments<T>(json));

        /// <summary>
        /// Contract for a multi-search response, each result holding <see cref="JsonDocument"/> hits.
        /// </summary>
        public JsonTypeInfo<MultiSearchEnvelope> MultiSearchEnvelopeInfo()
            => Built(WriteNulls, this, static (_, json) => EnvelopeContracts.MultiSearch(json));

        /// <summary>
        /// Returns the cached metadata for <typeparamref name="TInfo"/> under <paramref name="options"/>, building it
        /// with <paramref name="build"/> on first use. The build runs before the cache is touched so an
        /// unregistered document type fails with the resolver's error. <paramref name="state"/> is passed through so
        /// callers can use non-capturing delegates.
        /// </summary>
        private JsonTypeInfo<TInfo> Built<TInfo, TState>(JsonSerializerOptions options, TState state, Func<JsonSerializerOptions, TState, JsonTypeInfo<TInfo>> build)
        {
            var key = (options, typeof(TInfo));
            if (_built.TryGetValue(key, out var cached))
            {
                return (JsonTypeInfo<TInfo>)cached;
            }

            var info = build(options, state);
            info.MakeReadOnly();
            return (JsonTypeInfo<TInfo>)_built.GetOrAdd(key, info);
        }

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
