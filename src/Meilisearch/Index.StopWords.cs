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
        /// Gets the stop words setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the stop words setting.</returns>
        public async Task<IEnumerable<string>> GetStopWordsAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/stop-words", _json.Info<IEnumerable<string>>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the stop words setting.
        /// </summary>
        /// <param name="stopWords">Collection of stop words.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdateStopWordsAsync(IEnumerable<string> stopWords, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PutJsonAsync($"indexes/{Uid}/settings/stop-words", stopWords, _json.RemoveNulls, cancellationToken)
                    .ConfigureAwait(false);
            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the stop words setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetStopWordsAsync(CancellationToken cancellationToken = default)
        {
            var httpresponse = await _http.DeleteAsync($"indexes/{Uid}/settings/stop-words", cancellationToken)
                .ConfigureAwait(false);
            return await httpresponse.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
