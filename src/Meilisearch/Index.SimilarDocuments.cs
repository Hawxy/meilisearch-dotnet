using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Extensions;
using Meilisearch.Json;

namespace Meilisearch
{
    public partial class Index
    {
        /// <summary>
        /// Search for similar documents.
        /// </summary>
        /// <param name="query">The query to search for similar documents.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <typeparam name="T">The type of the documents to return.</typeparam>
        /// <returns>Returns the similar documents.</returns>
        public async Task<SimilarDocumentsResult<T>> SearchSimilarDocumentsAsync<T>(
            SimilarDocumentsQuery query,
            CancellationToken cancellationToken = default)
        {
            var responseMessage = await _http
                .PostJsonAsync($"indexes/{Uid}/similar", query, _json.RemoveNulls, cancellationToken)
                .ConfigureAwait(false);

            var envelope = await responseMessage.Content
                .ReadFromJsonAsync(_json.SimilarDocumentsEnvelopeInfo<T>(), cancellationToken)
                .ConfigureAwait(false);


            return envelope?.ToResult();
        }
    }
}
