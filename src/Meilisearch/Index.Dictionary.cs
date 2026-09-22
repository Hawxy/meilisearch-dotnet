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
        /// Gets the dictionary of an index.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the dictionary.</returns>
        public async Task<IEnumerable<string>> GetDictionaryAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/dictionary", _json.Info<string[]>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the dictionary of an index.
        /// </summary>
        /// <param name="dictionary">Dictionary object.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdateDictionaryAsync(IEnumerable<string> dictionary, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PutJsonAsync($"indexes/{Uid}/settings/dictionary", dictionary, _json.WriteNulls, cancellationToken)
                    .ConfigureAwait(false);
            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the dictionary to their default values.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetDictionaryAsync(CancellationToken cancellationToken = default)
        {
            var httpResponse = await _http.DeleteAsync($"indexes/{Uid}/settings/dictionary", cancellationToken).ConfigureAwait(false);
            return await httpResponse.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
