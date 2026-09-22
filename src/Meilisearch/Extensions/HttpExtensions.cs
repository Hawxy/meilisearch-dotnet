using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

using Meilisearch.Json;

namespace Meilisearch.Extensions
{
    /// <summary>
    /// JSON request helpers. Bodies are sent as application/json without a charset parameter.
    /// </summary>
    internal static class HttpExtensions
    {
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

        /// <summary>
        /// POSTs <paramref name="body"/> as JSON using the metadata that <paramref name="options"/> resolves for <typeparamref name="T"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken)
            => client.PostJsonAsync(uri, body, MeilisearchJson.TypeInfo<T>(options), cancellationToken);

        /// <summary>
        /// POSTs <paramref name="body"/> as JSON described by <paramref name="typeInfo"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string uri, T body, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
            => client.SendJsonAsync(HttpMethod.Post, uri, CreateJsonContent(body, typeInfo), cancellationToken);

        /// <summary>
        /// PUTs <paramref name="body"/> as JSON using the metadata that <paramref name="options"/> resolves for <typeparamref name="T"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken)
            => client.PutJsonAsync(uri, body, MeilisearchJson.TypeInfo<T>(options), cancellationToken);

        /// <summary>
        /// PUTs <paramref name="body"/> as JSON described by <paramref name="typeInfo"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string uri, T body, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
            => client.SendJsonAsync(HttpMethod.Put, uri, CreateJsonContent(body, typeInfo), cancellationToken);

        /// <summary>
        /// PATCHes <paramref name="body"/> as JSON using the metadata that <paramref name="options"/> resolves for <typeparamref name="T"/>.
        /// </summary>
        internal static Task<HttpResponseMessage> PatchJsonAsync<T>(this HttpClient client, string uri, T body, JsonSerializerOptions options, CancellationToken cancellationToken)
            => client.SendJsonAsync(Patch, uri, CreateJsonContent(body, MeilisearchJson.TypeInfo<T>(options)), cancellationToken);

        private static readonly HttpMethod Patch = new HttpMethod("PATCH");

        /// <summary>
        /// Sends a JSON request and returns once the response headers arrive, so the body is parsed from the
        /// network stream instead of being buffered first.
        /// </summary>
        private static Task<HttpResponseMessage> SendJsonAsync(this HttpClient client, HttpMethod method, string uri, HttpContent content, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(method, uri) { Content = content };
            return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        private static JsonContent CreateJsonContent<T>(T body, JsonTypeInfo<T> typeInfo)
            => JsonContent.Create(body, typeInfo, new MediaTypeHeaderValue("application/json"));
    }
}
