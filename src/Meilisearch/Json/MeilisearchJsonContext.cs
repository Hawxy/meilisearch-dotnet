using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Meilisearch.QueryParameters;

namespace Meilisearch.Json
{
    /// <summary>
    /// Source-generated JSON metadata for every type the SDK sends to or receives from Meilisearch.
    /// Used as a type info resolver on the client's options, so the runtime options decide naming and casing.
    /// </summary>
    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Metadata,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowOutOfOrderMetadataProperties = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    // Server and index
    [JsonSerializable(typeof(MeiliSearchHealth))]
    [JsonSerializable(typeof(MeiliSearchVersion))]
    [JsonSerializable(typeof(MeilisearchApiErrorContent))]
    [JsonSerializable(typeof(Index))]
    [JsonSerializable(typeof(ResourceResults<IEnumerable<Index>>))]
    [JsonSerializable(typeof(IndexStats))]
    [JsonSerializable(typeof(Stats))]
    [JsonSerializable(typeof(IndexSwap))]
    [JsonSerializable(typeof(List<IndexSwap>))]
    [JsonSerializable(typeof(IndexPrimaryKeyPatch))]
    [JsonSerializable(typeof(ExperimentalFeaturesPatch))]
    // Tasks
    [JsonSerializable(typeof(TaskInfo))]
    [JsonSerializable(typeof(TaskInfoStatus))]
    [JsonSerializable(typeof(TaskInfoType))]
    [JsonSerializable(typeof(TaskResource))]
    [JsonSerializable(typeof(TasksResults<IEnumerable<TaskResource>>))]
    // Keys
    [JsonSerializable(typeof(Key))]
    [JsonSerializable(typeof(KeyAction))]
    [JsonSerializable(typeof(ResourceResults<IEnumerable<Key>>))]
    // Settings
    [JsonSerializable(typeof(Settings))]
    [JsonSerializable(typeof(TypoTolerance))]
    [JsonSerializable(typeof(TypoTolerance.TypoSize))]
    [JsonSerializable(typeof(Faceting))]
    [JsonSerializable(typeof(SortFacetValuesByType))]
    [JsonSerializable(typeof(Pagination))]
    [JsonSerializable(typeof(FilterableAttribute))]
    [JsonSerializable(typeof(FilterableAttributeFeatures))]
    [JsonSerializable(typeof(FilterableAttributeFilterFeatures))]
    [JsonSerializable(typeof(IEnumerable<FilterableAttribute>))]
    [JsonSerializable(typeof(Embedder))]
    [JsonSerializable(typeof(EmbedderSource))]
    [JsonSerializable(typeof(EmbedderDistribution))]
    [JsonSerializable(typeof(Dictionary<string, Embedder>))]
    [JsonSerializable(typeof(Dictionary<string, IEnumerable<string>>))]
    // Search
    [JsonSerializable(typeof(SearchQuery))]
    [JsonSerializable(typeof(FederatedSearchQuery))]
    [JsonSerializable(typeof(MultiSearchQuery))]
    [JsonSerializable(typeof(FederatedMultiSearchQuery))]
    [JsonSerializable(typeof(MultiSearchFederationOptions))]
    [JsonSerializable(typeof(HybridSearch))]
    [JsonSerializable(typeof(FacetSearchQuery))]
    [JsonSerializable(typeof(FacetSearchResult))]
    [JsonSerializable(typeof(FacetSearchResult.FacetHit))]
    [JsonSerializable(typeof(FacetStat))]
    [JsonSerializable(typeof(MatchPosition))]
    [JsonSerializable(typeof(SimilarDocumentsQuery))]
    // Response envelopes whose items are deserialized separately with caller metadata
    [JsonSerializable(typeof(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>))]
    [JsonSerializable(typeof(IReadOnlyDictionary<string, IReadOnlyCollection<MatchPosition>>))]
    [JsonSerializable(typeof(IReadOnlyDictionary<string, FacetStat>))]
    // Documents
    [JsonSerializable(typeof(DocumentsQuery))]
    [JsonSerializable(typeof(DeleteDocumentsQuery))]
    // Dynamic search rules
    [JsonSerializable(typeof(DynamicSearchRule))]
    [JsonSerializable(typeof(ResourceResults<IEnumerable<DynamicSearchRule>>))]
    [JsonSerializable(typeof(PatchDynamicSearchRule))]
    [JsonSerializable(typeof(DSRAction))]
    [JsonSerializable(typeof(IEnumerable<DSRAction>))]
    [JsonSerializable(typeof(DSRASelector))]
    [JsonSerializable(typeof(BaseAction))]
    [JsonSerializable(typeof(PinAction))]
    [JsonSerializable(typeof(ActionType))]
    [JsonSerializable(typeof(DynamicSearchRuleConditions))]
    [JsonSerializable(typeof(QueryCondition))]
    [JsonSerializable(typeof(TimeCondition))]
    [JsonSerializable(typeof(FilterCondition))]
    [JsonSerializable(typeof(DynamicSearchRulesQuery))]
    [JsonSerializable(typeof(DynamicSearchRulesQuery.DSRQFilter))]
    // Bare payloads used by settings endpoints
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(bool?))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(int?))]
    [JsonSerializable(typeof(ulong?))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(IEnumerable<string>))]
    // Untyped documents
    [JsonSerializable(typeof(object))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonDocument))]
    [JsonSerializable(typeof(JsonNode))]
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(JsonArray))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    internal sealed partial class MeilisearchJsonContext : JsonSerializerContext
    {
    }
}
