using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Extensions;
namespace Meilisearch
{
    public partial class Index
    {
        /// <summary>
        /// Gets the faceting setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the faceting setting.</returns>
        public async Task<Faceting> GetFacetingAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/faceting", _json.Info<Faceting>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the faceting setting.
        /// </summary>
        /// <param name="faceting">Faceting instance</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdateFacetingAsync(Faceting faceting, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PatchJsonAsync($"indexes/{Uid}/settings/faceting", faceting, _json.RemoveNulls, cancellationToken)
                    .ConfigureAwait(false);

            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the faceting setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetFacetingAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.DeleteAsync($"indexes/{Uid}/settings/faceting", cancellationToken)
                .ConfigureAwait(false);

            return await response.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
