using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Extensions;

namespace Meilisearch
{
    public partial class Index
    {
        /// <summary>
        /// Gets the prefix search setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the prefix search setting.</returns>
        public async Task<string> GetPrefixSearchAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/prefix-search", _json.Info<string>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the prefix search setting.
        /// </summary>
        /// <param name="prefixSearch">The new prefix search setting (e.g. "indexingTime" or "disabled").</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdatePrefixSearchAsync(string prefixSearch, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PutJsonAsync($"indexes/{Uid}/settings/prefix-search", prefixSearch, _json.RemoveNulls, cancellationToken)
                    .ConfigureAwait(false);

            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the prefix search setting. (default: "indexingTime")
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetPrefixSearchAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.DeleteAsync($"indexes/{Uid}/settings/prefix-search", cancellationToken)
                .ConfigureAwait(false);

            return await response.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
