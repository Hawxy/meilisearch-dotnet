using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Extensions;

namespace Meilisearch
{
    public partial class Index
    {
        /// <summary>
        /// Gets all the non separator tokens settings.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns all the configured non separator tokens.</returns>
        public async Task<List<string>> GetNonSeparatorTokensAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/non-separator-tokens", _json.Info<List<string>>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates all the non separator tokens settings.
        /// </summary>
        /// <param name="nonSeparatorTokens">Collection of separator tokens.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdateNonSeparatorTokensAsync(IEnumerable<string> nonSeparatorTokens, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PutJsonAsync($"indexes/{Uid}/settings/non-separator-tokens", nonSeparatorTokens, _json.RemoveNulls, cancellationToken)
                    .ConfigureAwait(false);
            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resets all the non separator tokens settings.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetNonSeparatorTokensAsync(CancellationToken cancellationToken = default)
        {
            var httpresponse = await _http.DeleteAsync($"indexes/{Uid}/settings/non-separator-tokens", cancellationToken)
                .ConfigureAwait(false);
            return await httpresponse.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
