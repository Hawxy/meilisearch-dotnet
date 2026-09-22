using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Json;

namespace Meilisearch.Extensions
{
    /// <summary>
    /// Class to communicate with the Meilisearch server without charset-utf-8 as Content-Type.
    /// </summary>
    internal static class HttpExtensions
    {
        /// <summary>
        /// Sends JSON payload using POST without "charset-utf-8" as Content-Type.
        /// </summary>
        /// <param name="client">HttpClient.</param>
        /// <param name="uri">Endpoint.</param>
        /// <param name="body">Body sent.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <typeparam name="T">Type of the body to send.</typeparam>
        /// <returns>Returns the HTTP response from the Meilisearch server.</returns>
        internal static async Task<HttpResponseMessage> PostJsonCustomAsync<T>(this HttpClient client, string uri, T body, CancellationToken cancellationToken = default)
        {
            var payload = PrepareJsonPayload(body);

            return await client.PostAsync(uri, payload, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends JSON payload using POST without "charset-utf-8" as Content-Type and using JSON serializer options.
        /// </summary>
        /// <param name="client">HttpClient.</param>
        /// <param name="uri">Endpoint.</param>
        /// <param name="body">Body sent.</param>
        /// <param name="options">Json options for serialization.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <typeparam name="T">Type of the body to send.</typeparam>
        /// <returns>Returns the HTTP response from the Meilisearch server.</returns>
        internal static async Task<HttpResponseMessage> PostJsonCustomAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken = default)
        {
            var payload = PrepareJsonPayload(body, options);

            return await client.PostAsync(uri, payload, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends JSON payload using PUT without "charset-utf-8" as Content-Type.
        /// </summary>
        /// <param name="client">HttpClient.</param>
        /// <param name="uri">Endpoint.</param>
        /// <param name="body">Body sent.</param>
        /// <param name="options">Json options for serialization.</param>
        /// <param name="cancellationToken">The cancellation token for this call.</param>
        /// <typeparam name="T">Type of the body to send.</typeparam>
        /// <returns>Returns the HTTP response from the Meilisearch server.</returns>
        internal static async Task<HttpResponseMessage> PutJsonCustomAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken = default)
        {
            var payload = PrepareJsonPayload(body, options);

            return await client.PutAsync(uri, payload, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Add the API Key to the Authorization header.
        /// </summary>
        /// <param name="client">HttpClient.</param>
        /// <param name="apiKey">API Key.</param>
        internal static void AddApiKeyToHeader(this HttpClient client, string apiKey)
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        internal static void AddDefaultUserAgent(this HttpClient client)
        {
            var version = new Version();

            client.DefaultRequestHeaders.Add("User-Agent", version.GetQualifiedVersion());
        }

        private static JsonContent PrepareJsonPayload<T>(T body, JsonSerializerOptions options = null)
        {
            options = options ?? Constants.JsonSerializerOptionsWriteNulls;
            var payload = JsonContent.Create(body, new MediaTypeHeaderValue("application/json"), options);

            return payload;
        }

        /// <summary>
        /// POSTs <paramref name="body"/> as JSON using the metadata that <paramref name="options"/> resolves for <typeparamref name="T"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken)
            => client.PostAsync(uri, CreateJsonContent(body, options), cancellationToken);

        /// <summary>
        /// PUTs <paramref name="body"/> as JSON using the metadata that <paramref name="options"/> resolves for <typeparamref name="T"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken)
            => client.PutAsync(uri, CreateJsonContent(body, options), cancellationToken);

        /// <summary>
        /// PATCHes <paramref name="body"/> as JSON using the metadata that <paramref name="options"/> resolves for <typeparamref name="T"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PatchJsonAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken)
            => client.PatchAsync(uri, CreateJsonContent(body, options), cancellationToken);

        private static JsonContent CreateJsonContent<T>(T body, JsonSerializerOptions options)
            => JsonContent.Create(body, MeilisearchJson.TypeInfo<T>(options), new MediaTypeHeaderValue("application/json"));
    }
}
