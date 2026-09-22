using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Meilisearch.Json;

using Xunit;

namespace Meilisearch.Tests
{
    public class EnvelopeTests
    {
        private static readonly MeilisearchJson Json = TestJson.SourceGenJson;

        private static ISearchable<Movie> ReadSearch(string json)
            => JsonSerializer.Deserialize(json, Json.SearchEnvelopeInfo<Movie>())?.ToSearchable();

        [Fact]
        public void PlainSearchMapsToSearchResult()
        {
            var result = ReadSearch("{\"hits\":[{\"id\":\"1\",\"name\":\"Batman\"},{\"id\":\"2\"}],\"offset\":5,\"limit\":20,\"estimatedTotalHits\":42," +
                                    "\"processingTimeMs\":3,\"query\":\"bat\",\"indexUid\":\"movies\"," +
                                    "\"facetDistribution\":{\"genre\":{\"action\":2}},\"facetStats\":{\"year\":{\"min\":1,\"max\":9}}}");

            var plain = Assert.IsType<SearchResult<Movie>>(result);
            Assert.Equal(new[] { "1", "2" }, plain.Hits.Select(h => h.Id));
            Assert.Equal("Batman", plain.Hits.First().Name);
            Assert.Equal(5, plain.Offset);
            Assert.Equal(20, plain.Limit);
            Assert.Equal(42, plain.EstimatedTotalHits);
            Assert.Equal(3, plain.ProcessingTimeMs);
            Assert.Equal("bat", plain.Query);
            Assert.Equal("movies", plain.IndexUid);
            Assert.Equal(2, plain.FacetDistribution["genre"]["action"]);
            Assert.Equal(9, plain.FacetStats["year"].Max);
        }

        [Theory]
        [InlineData("{\"page\":2,\"hitsPerPage\":10,\"totalHits\":25,\"totalPages\":3,\"hits\":[{\"id\":\"1\"}],\"processingTimeMs\":1,\"query\":\"\"}")]
        [InlineData("{\"hits\":[{\"id\":\"1\"}],\"processingTimeMs\":1,\"query\":\"\",\"totalPages\":3,\"totalHits\":25,\"hitsPerPage\":10,\"page\":2}")]
        public void PaginatedSearchMapsToPaginatedResultInAnyPropertyOrder(string json)
        {
            var result = ReadSearch(json);

            var paginated = Assert.IsType<PaginatedSearchResult<Movie>>(result);
            Assert.Equal(2, paginated.Page);
            Assert.Equal(10, paginated.HitsPerPage);
            Assert.Equal(25, paginated.TotalHits);
            Assert.Equal(3, paginated.TotalPages);
            Assert.Equal("1", paginated.Hits.Single().Id);
        }

        [Fact]
        public void NullPaginationMembersMeanOffsetBased()
        {
            var result = ReadSearch("{\"hits\":[],\"page\":null,\"hitsPerPage\":null,\"offset\":0,\"limit\":20,\"estimatedTotalHits\":0,\"processingTimeMs\":0,\"query\":\"x\"}");

            Assert.IsType<SearchResult<Movie>>(result);
        }

        [Fact]
        public void OptionalEnvelopeMembersAndUnknownPropertiesAreTolerated()
        {
            var result = ReadSearch("{\"hits\":[],\"offset\":0,\"limit\":20,\"estimatedTotalHits\":0,\"processingTimeMs\":0,\"query\":\"x\",\"semanticHitCount\":0,\"newField\":{\"a\":[1,{\"b\":null}]},\"tail\":\"z\"}");

            var plain = Assert.IsType<SearchResult<Movie>>(result);
            Assert.Empty(plain.Hits);
            Assert.Null(plain.FacetDistribution);
            Assert.Null(plain.FacetStats);
            Assert.Null(plain.MatchesPosition);
            Assert.Null(plain.IndexUid);
        }

        [Fact]
        public void MatchesPositionIsRead()
        {
            var result = ReadSearch("{\"hits\":[{\"id\":\"1\"}],\"processingTimeMs\":0,\"query\":\"x\",\"_matchesPosition\":{\"name\":[{\"start\":0,\"length\":3}]}}");

            Assert.Equal(3, result.MatchesPosition["name"].Single().Length);
        }

        [Fact]
        public void PropertyNamesAreMatchedCaseInsensitively()
        {
            var result = ReadSearch("{\"Hits\":[{\"ID\":\"1\"}],\"ProcessingTimeMs\":4,\"Query\":\"x\"}");

            Assert.Equal("1", result.Hits.Single().Id);
            Assert.Equal(4, result.ProcessingTimeMs);
        }

        [Fact]
        public void MissingHitsMapToNull()
        {
            Assert.Null(ReadSearch("{\"processingTimeMs\":0,\"query\":\"x\"}").Hits);
            Assert.Null(ReadSearch("{\"hits\":null,\"processingTimeMs\":0,\"query\":\"x\"}").Hits);
        }

        [Fact]
        public void HitsThatAreNotAnArrayThrow()
        {
            Assert.Throws<JsonException>(() => ReadSearch("{\"hits\":{\"id\":\"1\"},\"processingTimeMs\":0,\"query\":\"x\"}"));
        }

        [Fact]
        public void EnvelopeThatIsNotAnObjectThrows()
        {
            Assert.Throws<JsonException>(() => ReadSearch("[]"));
        }

        [Fact]
        public void NullEnvelopeMapsToNull()
        {
            Assert.Null(ReadSearch("null"));
        }

        [Fact]
        public void UnregisteredHitTypeReportsTheTypeName()
        {
            var error = Assert.Throws<System.NotSupportedException>(() => Json.SearchEnvelopeInfo<UnregisteredDocument>());

            Assert.Contains(nameof(UnregisteredDocument), error.Message);
        }

        [Fact]
        public void EnvelopeInfoIsCachedPerDocumentType()
        {
            Assert.Same(Json.SearchEnvelopeInfo<Movie>(), Json.SearchEnvelopeInfo<Movie>());
            Assert.Same(Json.ResourceResultsEnvelopeInfo<Movie>(), Json.ResourceResultsEnvelopeInfo<Movie>());
            Assert.Same(Json.SimilarDocumentsEnvelopeInfo<Movie>(), Json.SimilarDocumentsEnvelopeInfo<Movie>());
            Assert.Same(Json.MultiSearchEnvelopeInfo(), Json.MultiSearchEnvelopeInfo());
            Assert.NotSame(Json.SearchEnvelopeInfo<Movie>(), Json.SearchEnvelopeInfo<MovieWithIntId>());
        }

        [Fact]
        public void ResourceResultsMapItemsAndPaging()
        {
            var results = JsonSerializer.Deserialize("{\"results\":[{\"id\":\"1\"},{\"id\":\"2\"}],\"offset\":10,\"limit\":2,\"total\":12}", Json.ResourceResultsEnvelopeInfo<Movie>()).ToResourceResults();

            Assert.Equal(new[] { "1", "2" }, results.Results.Select(m => m.Id));
            Assert.Equal(10, results.Offset);
            Assert.Equal(2, results.Limit);
            Assert.Equal(12, results.Total);
        }

        [Fact]
        public void ResourceResultsWithoutLimitKeepItNull()
        {
            var results = JsonSerializer.Deserialize("{\"results\":[],\"offset\":0,\"total\":0}", Json.ResourceResultsEnvelopeInfo<Movie>()).ToResourceResults();

            Assert.Null(results.Limit);
            Assert.Empty(results.Results);
        }

        [Fact]
        public void SimilarDocumentsMapHitsAndMetadata()
        {
            var result = JsonSerializer.Deserialize("{\"hits\":[{\"id\":\"2\"}],\"id\":\"1\",\"processingTimeMs\":4,\"offset\":0,\"limit\":20,\"estimatedTotalHits\":1}", Json.SimilarDocumentsEnvelopeInfo<Movie>()).ToResult();

            Assert.Equal("2", result.Hits.Single().Id);
            Assert.Equal("1", result.Id);
            Assert.Equal(4, result.ProcessingTimeMs);
            Assert.Equal(20, result.Limit);
            Assert.Equal(1, result.EstimatedTotalHits);
        }

        [Fact]
        public void MultiSearchMapsEachResultWithDocumentHits()
        {
            var result = JsonSerializer.Deserialize(
                "{\"results\":[{\"indexUid\":\"a\",\"hits\":[{\"id\":\"1\",\"nested\":{\"x\":[1,2]}}],\"processingTimeMs\":1,\"query\":\"\",\"offset\":0,\"limit\":20,\"estimatedTotalHits\":1}," +
                "{\"indexUid\":\"b\",\"hits\":[],\"processingTimeMs\":1,\"query\":\"\",\"page\":1,\"hitsPerPage\":5,\"totalHits\":0,\"totalPages\":0}]}",
                Json.MultiSearchEnvelopeInfo()).ToMultiSearchResult();

            Assert.Equal(2, result.Results.Count);
            var first = Assert.IsType<SearchResult<JsonDocument>>(result.Results[0]);
            Assert.Equal("a", first.IndexUid);
            Assert.Equal(2, first.Hits.Single().RootElement.GetProperty("nested").GetProperty("x").GetArrayLength());
            var second = Assert.IsType<PaginatedSearchResult<JsonDocument>>(result.Results[1]);
            Assert.Equal(5, second.HitsPerPage);
            Assert.Empty(second.Hits);
        }

        [Fact]
        public void EmptyOrMissingMultiSearchResultsMapToEmptyList()
        {
            Assert.Empty(JsonSerializer.Deserialize("{\"results\":[]}", Json.MultiSearchEnvelopeInfo()).ToMultiSearchResult().Results);
            Assert.Empty(JsonSerializer.Deserialize("{}", Json.MultiSearchEnvelopeInfo()).ToMultiSearchResult().Results);
        }

        [Fact]
        public void SearchableConverterRoundTripsConcreteTypes()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var paginated = new PaginatedSearchResult<Movie>(new List<Movie> { new Movie { Id = "1" } }, 10, 2, 25, 3, null, 1, "q", null, null, "movies");
            var plain = new SearchResult<Movie>(new List<Movie> { new Movie { Id = "1" } }, 0, 20, 1, null, 1, "q", null, null, "movies");

            var paginatedBack = JsonSerializer.Deserialize<ISearchable<Movie>>(JsonSerializer.Serialize<ISearchable<Movie>>(paginated, options), options);
            var plainBack = JsonSerializer.Deserialize<ISearchable<Movie>>(JsonSerializer.Serialize<ISearchable<Movie>>(plain, options), options);

            Assert.Equal(2, Assert.IsType<PaginatedSearchResult<Movie>>(paginatedBack).Page);
            Assert.Equal(20, Assert.IsType<SearchResult<Movie>>(plainBack).Limit);
        }

        public class UnregisteredDocument
        {
            public string Id { get; set; }
        }
    }
}
