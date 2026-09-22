using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Meilisearch.Json;

using Xunit;

namespace Meilisearch.Tests
{
    public class EnvelopeStreamingTests
    {
        private static readonly MeilisearchJson Json = TestJson.SourceGenJson;

        /// <summary>
        /// Delivers at most 100 bytes per read so the serializer has to resume across buffer refills.
        /// </summary>
        private sealed class TrickleStream : Stream
        {
            private readonly byte[] _data;
            private int _position;

            public TrickleStream(byte[] data) => _data = data;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _data.Length;
            public override long Position { get => _position; set => throw new System.NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new System.NotSupportedException();
            public override void SetLength(long value) => throw new System.NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new System.NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = System.Math.Min(System.Math.Min(count, 100), _data.Length - _position);
                System.Array.Copy(_data, _position, buffer, offset, n);
                _position += n;
                return n;
            }
        }

        [Fact]
        public async Task SearchEnvelopeLargerThanTheReadBufferIsReadFromAStream()
        {
            var hits = Enumerable.Range(0, 5000).Select(i => new Movie { Id = i.ToString(), Name = "Movie " + i, Genre = "genre-" + (i % 7) });
            var body = "{\"processingTimeMs\":7,\"hits\":" + JsonSerializer.Serialize(hits, Json.ItemsInfo<Movie>()) +
                       ",\"query\":\"q\",\"page\":3,\"hitsPerPage\":5000,\"totalHits\":15000,\"totalPages\":3,\"facetDistribution\":{\"genre\":{\"genre-1\":715}}}";
            var content = new StreamContent(new TrickleStream(Encoding.UTF8.GetBytes(body)));

            var result = (await content.ReadFromJsonAsync(Json.SearchEnvelopeInfo<Movie>())).ToSearchable();

            var paginated = Assert.IsType<PaginatedSearchResult<Movie>>(result);
            Assert.Equal(5000, paginated.Hits.Count);
            Assert.Equal("4999", paginated.Hits.Last().Id);
            Assert.Equal(3, paginated.Page);
            Assert.Equal(715, paginated.FacetDistribution["genre"]["genre-1"]);
        }

        [Fact]
        public async Task DocumentsEnvelopeIsReadFromAStream()
        {
            var body = "{\"results\":" + JsonSerializer.Serialize(Enumerable.Range(0, 2000).Select(i => new Movie { Id = i.ToString() }), Json.ItemsInfo<Movie>()) + ",\"offset\":20,\"limit\":2000,\"total\":9999}";
            var content = new StreamContent(new TrickleStream(Encoding.UTF8.GetBytes(body)));

            var result = (await content.ReadFromJsonAsync(Json.ResourceResultsEnvelopeInfo<Movie>())).ToResourceResults();

            Assert.Equal(2000, result.Results.Count());
            Assert.Equal(20, result.Offset);
            Assert.Equal(9999, result.Total);
        }
    }
}
