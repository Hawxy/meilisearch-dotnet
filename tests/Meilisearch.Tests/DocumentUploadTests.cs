using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Meilisearch.Tests
{
    public class DocumentUploadTests
    {
        private const string TaskResponse =
            "{\"taskUid\":7,\"indexUid\":\"movies\",\"status\":\"enqueued\",\"type\":\"documentAdditionOrUpdate\",\"enqueuedAt\":\"2024-01-01T00:00:00Z\"}";

        private const string SearchResponse =
            "{\"hits\":[{\"id\":\"1\",\"name\":\"Batman\"}],\"offset\":0,\"limit\":5,\"estimatedTotalHits\":1,\"processingTimeMs\":1,\"query\":\"bat\"}";

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly string _response;

            public RecordingHandler(string response) => _response = response;

            public HttpRequestMessage Request { get; private set; }
            public string Body { get; private set; }
            public string ContentType { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;
                Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                ContentType = request.Content?.Headers.ContentType?.ToString();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_response, Encoding.UTF8, "application/json"),
                };
            }
        }

        private static (Index Index, RecordingHandler Handler) CreateIndex(bool sourceGen, string response = TaskResponse)
        {
            var handler = new RecordingHandler(response);
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:7700/") };
            var client = sourceGen
                ? new MeilisearchClient(http, "key", TestJson.SourceGen)
                : new MeilisearchClient(http, "key");
            return (client.Index("movies"), handler);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AddDocumentsWritesArrayWithNulls(bool sourceGen)
        {
            var (index, handler) = CreateIndex(sourceGen);

            var task = await index.AddDocumentsAsync(new[] { new Movie { Id = "1", Name = "Batman" } }, "id");

            Assert.Equal(7, task.TaskUid);
            Assert.Equal(HttpMethod.Post, handler.Request.Method);
            Assert.Equal("/indexes/movies/documents?primaryKey=id", handler.Request.RequestUri.PathAndQuery);
            Assert.Equal("application/json", handler.ContentType);
            Assert.Equal("[{\"id\":\"1\",\"name\":\"Batman\",\"genre\":null}]", handler.Body);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateDocumentsWritesArrayWithoutNulls(bool sourceGen)
        {
            var (index, handler) = CreateIndex(sourceGen);

            await index.UpdateDocumentsAsync(new List<Movie> { new Movie { Id = "1" }, new Movie { Id = "2", Genre = "SF" } });

            Assert.Equal(HttpMethod.Put, handler.Request.Method);
            Assert.Equal("/indexes/movies/documents", handler.Request.RequestUri.PathAndQuery);
            Assert.Equal("application/json", handler.ContentType);
            Assert.Equal("[{\"id\":\"1\"},{\"id\":\"2\",\"genre\":\"SF\"}]", handler.Body);
        }

        [Fact]
        public async Task EmptyDocumentsWriteEmptyArray()
        {
            var (index, handler) = CreateIndex(true);

            await index.AddDocumentsAsync(Array.Empty<Movie>());

            Assert.Equal("[]", handler.Body);
        }

        [Fact]
        public async Task SourceGenClientRejectsUnregisteredDocumentTypes()
        {
            var (index, _) = CreateIndex(true);

            var error = await Assert.ThrowsAsync<NotSupportedException>(() => index.AddDocumentsAsync(new[] { new EnvelopeTests.UnregisteredDocument() }));

            Assert.Contains(nameof(EnvelopeTests.UnregisteredDocument), error.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SearchOmitsNullsInBodyAndReadsTypedHits(bool sourceGen)
        {
            var (index, handler) = CreateIndex(sourceGen, SearchResponse);

            var result = await index.SearchAsync<Movie>("bat", new SearchQuery { Limit = 5 });

            Assert.Equal("application/json", handler.ContentType);
            Assert.Contains("\"q\":\"bat\"", handler.Body);
            Assert.Contains("\"limit\":5", handler.Body);
            Assert.DoesNotContain("\"filter\"", handler.Body);
            Assert.DoesNotContain("\"indexUid\"", handler.Body);
            Assert.Equal("Batman", Assert.IsType<SearchResult<Movie>>(result).Hits.Single().Name);
        }
    }
}
