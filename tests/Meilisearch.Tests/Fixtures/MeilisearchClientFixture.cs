using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Xunit;

namespace Meilisearch.Tests.Fixtures
{
    public abstract class MeilisearchClientFixture : IAsyncLifetime
    {
        public MeilisearchClientFixture()
        {
            var options = JsonOptions();
            var httpClient = new HttpClient(new MeilisearchMessageHandler(new HttpClientHandler())) { BaseAddress = new Uri(MeilisearchAddress()) };

            if (options == null)
            {
                DefaultClient = new MeilisearchClient(MeilisearchAddress(), ApiKey);
                ClientWithCustomHttpClient = new MeilisearchClient(httpClient, ApiKey);
            }
            else
            {
                DefaultClient = new MeilisearchClient(MeilisearchAddress(), ApiKey, options);
                ClientWithCustomHttpClient = new MeilisearchClient(httpClient, ApiKey, options);
            }
        }

        private const string ApiKey = "masterKey";

        public virtual string MeilisearchAddress()
        {
            throw new InvalidOperationException("Please override the MeilisearchAddress property in inhereted class.");
        }

        /// <summary>
        /// Serializer options for the clients. Null selects the reflection-based constructors.
        /// </summary>
        public virtual JsonSerializerOptions JsonOptions() => null;

        public MeilisearchClient DefaultClient { get; private set; }
        public MeilisearchClient ClientWithCustomHttpClient { get; private set; }

        public virtual Task InitializeAsync() => Task.CompletedTask;

        public abstract Task DisposeAsync();
    }
}
