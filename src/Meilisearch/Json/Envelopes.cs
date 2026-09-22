using System.Collections.Generic;
using System.Text.Json;

namespace Meilisearch.Json
{
    /// <summary>
    /// Search response. Covers both the offset-based and the page-based shapes.
    /// </summary>
    internal sealed class SearchEnvelope<T>
    {
        public List<T> Hits { get; set; }
        public int? Offset { get; set; }
        public int? Limit { get; set; }
        public int? EstimatedTotalHits { get; set; }
        public int? HitsPerPage { get; set; }
        public int? Page { get; set; }
        public int? TotalHits { get; set; }
        public int? TotalPages { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string Query { get; set; }
        public string IndexUid { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> FacetDistribution { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyCollection<MatchPosition>> MatchesPosition { get; set; }
        public IReadOnlyDictionary<string, FacetStat> FacetStats { get; set; }

        /// <summary>
        /// A paginated result when the server answered with finite pagination, a plain result otherwise.
        /// </summary>
        public ISearchable<T> ToSearchable()
        {
            if (Page.HasValue || HitsPerPage.HasValue)
            {
                return new PaginatedSearchResult<T>(Hits, HitsPerPage ?? 0, Page ?? 0, TotalHits ?? 0, TotalPages ?? 0,
                    FacetDistribution, ProcessingTimeMs, Query, MatchesPosition, FacetStats, IndexUid);
            }

            return new SearchResult<T>(Hits, Offset ?? 0, Limit ?? 0, EstimatedTotalHits ?? 0,
                FacetDistribution, ProcessingTimeMs, Query, MatchesPosition, FacetStats, IndexUid);
        }
    }

    /// <summary>
    /// Paginated list response.
    /// </summary>
    internal sealed class ResourceResultsEnvelope<T>
    {
        public List<T> Results { get; set; }
        public int? Limit { get; set; }
        public int Offset { get; set; }
        public int Total { get; set; }

        public ResourceResults<IEnumerable<T>> ToResourceResults()
            => new ResourceResults<IEnumerable<T>>(Results, Limit, Offset, Total);
    }

    /// <summary>
    /// Similar-documents response.
    /// </summary>
    internal sealed class SimilarDocumentsEnvelope<T>
    {
        public List<T> Hits { get; set; }
        public string Id { get; set; }
        public int ProcessingTimeMs { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int EstimatedTotalHits { get; set; }

        public SimilarDocumentsResult<T> ToResult()
            => new SimilarDocumentsResult<T>(Hits, Id, ProcessingTimeMs, Offset, Limit, EstimatedTotalHits);
    }

    /// <summary>
    /// Multi-search response, each result carrying its hits as <see cref="JsonDocument"/>.
    /// </summary>
    internal sealed class MultiSearchEnvelope
    {
        public List<SearchEnvelope<JsonDocument>> Results { get; set; }

        public MultiSearchResult ToMultiSearchResult()
        {
            var results = new List<ISearchable<JsonDocument>>(Results?.Count ?? 0);
            if (Results != null)
            {
                foreach (var result in Results)
                {
                    results.Add(result.ToSearchable());
                }
            }

            return new MultiSearchResult { Results = results };
        }
    }
}
