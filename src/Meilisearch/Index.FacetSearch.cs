using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Extensions;

namespace Meilisearch
{
    public partial class Index
    {
        /// <summary>
        /// Gets whether facet search is enabled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns whether facet search is enabled.</returns>
        public async Task<bool> GetFacetSearchAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/facet-search", _json.Info<bool>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Enables or disables facet search.
        /// </summary>
        /// <param name="facetSearch">Whether facet search is enabled.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdateFacetSearchAsync(bool facetSearch, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PutJsonAsync($"indexes/{Uid}/settings/facet-search", facetSearch, _json.RemoveNulls, cancellationToken)
                    .ConfigureAwait(false);

            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the facet search setting. (default: true)
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetFacetSearchAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.DeleteAsync($"indexes/{Uid}/settings/facet-search", cancellationToken)
                .ConfigureAwait(false);

            return await response.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
