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
        /// Gets the ranking rules setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the ranking rules setting.</returns>
        public async Task<IEnumerable<string>> GetRankingRulesAsync(CancellationToken cancellationToken = default)
        {
            return await _http.GetFromJsonAsync($"indexes/{Uid}/settings/ranking-rules", _json.Info<IEnumerable<string>>(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the ranking rules setting.
        /// </summary>
        /// <param name="rankingRules">Collection of ranking rules.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> UpdateRankingRulesAsync(IEnumerable<string> rankingRules, CancellationToken cancellationToken = default)
        {
            var responseMessage =
                await _http.PutJsonAsync($"indexes/{Uid}/settings/ranking-rules", rankingRules, _json.RemoveNulls, cancellationToken)
                    .ConfigureAwait(false);
            return await responseMessage.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resets the ranking rules setting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <returns>Returns the task info of the asynchronous task.</returns>
        public async Task<TaskInfo> ResetRankingRulesAsync(CancellationToken cancellationToken = default)
        {
            var httpresponse = await _http.DeleteAsync($"indexes/{Uid}/settings/ranking-rules", cancellationToken)
                .ConfigureAwait(false);
            return await httpresponse.Content.ReadFromJsonAsync(_json.Info<TaskInfo>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
