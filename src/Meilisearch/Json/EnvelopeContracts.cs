using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Meilisearch.Json
{
    /// <summary>
    /// Builds serializer contracts for the envelope types by hand, because their hit lists are typed by the
    /// caller's document type and cannot be registered in <see cref="MeilisearchJsonContext"/>. The result is
    /// an ordinary object contract, so responses stream through the serializer in one pass.
    /// </summary>
    internal static class EnvelopeContracts
    {
        public static JsonTypeInfo<SearchEnvelope<T>> Search<T>(MeilisearchJson json)
        {
            var contract = new Contract<SearchEnvelope<T>>(json.WriteNulls);
            contract.Add("hits", json.ListInfo<T>(), e => e.Hits, (e, v) => e.Hits = v);
            contract.Add("offset", json.Info<int?>(), e => e.Offset, (e, v) => e.Offset = v);
            contract.Add("limit", json.Info<int?>(), e => e.Limit, (e, v) => e.Limit = v);
            contract.Add("estimatedTotalHits", json.Info<int?>(), e => e.EstimatedTotalHits, (e, v) => e.EstimatedTotalHits = v);
            contract.Add("hitsPerPage", json.Info<int?>(), e => e.HitsPerPage, (e, v) => e.HitsPerPage = v);
            contract.Add("page", json.Info<int?>(), e => e.Page, (e, v) => e.Page = v);
            contract.Add("totalHits", json.Info<int?>(), e => e.TotalHits, (e, v) => e.TotalHits = v);
            contract.Add("totalPages", json.Info<int?>(), e => e.TotalPages, (e, v) => e.TotalPages = v);
            contract.Add("processingTimeMs", json.Info<int>(), e => e.ProcessingTimeMs, (e, v) => e.ProcessingTimeMs = v);
            contract.Add("query", json.Info<string>(), e => e.Query, (e, v) => e.Query = v);
            contract.Add("indexUid", json.Info<string>(), e => e.IndexUid, (e, v) => e.IndexUid = v);
            contract.Add("facetDistribution", json.Info<IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>>(), e => e.FacetDistribution, (e, v) => e.FacetDistribution = v);
            contract.Add("_matchesPosition", json.Info<IReadOnlyDictionary<string, IReadOnlyCollection<MatchPosition>>>(), e => e.MatchesPosition, (e, v) => e.MatchesPosition = v);
            contract.Add("facetStats", json.Info<IReadOnlyDictionary<string, FacetStat>>(), e => e.FacetStats, (e, v) => e.FacetStats = v);
            return contract.Build(() => new SearchEnvelope<T>());
        }

        public static JsonTypeInfo<ResourceResultsEnvelope<T>> ResourceResults<T>(MeilisearchJson json)
        {
            var contract = new Contract<ResourceResultsEnvelope<T>>(json.WriteNulls);
            contract.Add("results", json.ListInfo<T>(), e => e.Results, (e, v) => e.Results = v);
            contract.Add("limit", json.Info<int?>(), e => e.Limit, (e, v) => e.Limit = v);
            contract.Add("offset", json.Info<int>(), e => e.Offset, (e, v) => e.Offset = v);
            contract.Add("total", json.Info<int>(), e => e.Total, (e, v) => e.Total = v);
            return contract.Build(() => new ResourceResultsEnvelope<T>());
        }

        public static JsonTypeInfo<SimilarDocumentsEnvelope<T>> SimilarDocuments<T>(MeilisearchJson json)
        {
            var contract = new Contract<SimilarDocumentsEnvelope<T>>(json.WriteNulls);
            contract.Add("hits", json.ListInfo<T>(), e => e.Hits, (e, v) => e.Hits = v);
            contract.Add("id", json.Info<string>(), e => e.Id, (e, v) => e.Id = v);
            contract.Add("processingTimeMs", json.Info<int>(), e => e.ProcessingTimeMs, (e, v) => e.ProcessingTimeMs = v);
            contract.Add("offset", json.Info<int>(), e => e.Offset, (e, v) => e.Offset = v);
            contract.Add("limit", json.Info<int>(), e => e.Limit, (e, v) => e.Limit = v);
            contract.Add("estimatedTotalHits", json.Info<int>(), e => e.EstimatedTotalHits, (e, v) => e.EstimatedTotalHits = v);
            return contract.Build(() => new SimilarDocumentsEnvelope<T>());
        }

        public static JsonTypeInfo<MultiSearchEnvelope> MultiSearch(MeilisearchJson json)
        {
            var contract = new Contract<MultiSearchEnvelope>(json.WriteNulls);
            contract.Add("results", json.ListInfo(json.SearchEnvelopeInfo<JsonDocument>()), e => e.Results, (e, v) => e.Results = v);
            return contract.Build(() => new MultiSearchEnvelope());
        }

        private sealed class Contract<TEnvelope>
        {
            private readonly JsonSerializerOptions _options;
            private readonly List<JsonPropertyInfo> _properties = new List<JsonPropertyInfo>();

            public Contract(JsonSerializerOptions options)
            {
                _options = options;
            }

            public void Add<TValue>(string name, JsonTypeInfo<TValue> typeInfo, Func<TEnvelope, TValue> get, Action<TEnvelope, TValue> set)
            {
                _properties.Add(JsonMetadataServices.CreatePropertyInfo(_options, new JsonPropertyInfoValues<TValue>
                {
                    IsProperty = true,
                    IsPublic = true,
                    DeclaringType = typeof(TEnvelope),
                    PropertyTypeInfo = typeInfo,
                    Getter = o => get((TEnvelope)o),
                    Setter = (o, v) => set((TEnvelope)o, v),
                    PropertyName = name,
                    JsonPropertyName = name,
                }));
            }

            public JsonTypeInfo<TEnvelope> Build(Func<TEnvelope> create)
            {
                var properties = _properties.ToArray();
                return JsonMetadataServices.CreateObjectInfo(_options, new JsonObjectInfoValues<TEnvelope>
                {
                    ObjectCreator = create,
                    PropertyMetadataInitializer = _ => properties,
                });
            }
        }
    }
}
