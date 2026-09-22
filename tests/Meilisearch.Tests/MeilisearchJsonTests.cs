using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Meilisearch.Json;

using Xunit;

namespace Meilisearch.Tests
{
    public class MeilisearchJsonTests
    {
        public static IEnumerable<object[]> RegisteredTypes()
            => typeof(MeilisearchJsonContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(JsonTypeInfo).IsAssignableFrom(p.PropertyType))
                .Select(p => ((JsonTypeInfo)p.GetValue(MeilisearchJsonContext.Default)).Type)
                .Select(t => new object[] { t });

        [Theory]
        [MemberData(nameof(RegisteredTypes))]
        public void DefaultResolvesEveryRegisteredTypeWithoutReflection(Type type)
        {
            Assert.NotNull(MeilisearchJson.Default.WriteNulls.GetTypeInfo(type));
            Assert.NotNull(MeilisearchJson.Default.RemoveNulls.GetTypeInfo(type));
        }

        [Fact]
        public void DefaultDoesNotResolveUnregisteredTypes()
        {
            var error = Assert.Throws<NotSupportedException>(() => MeilisearchJson.Default.Info<Movie>());

            Assert.Contains(nameof(Movie), error.Message);
            Assert.Contains("JsonSerializerContext", error.Message);
        }

        [Fact]
        public void ReflectionDefaultIsSharedAcrossClients()
        {
            Assert.Same(MeilisearchJson.ReflectionDefault(), MeilisearchJson.ReflectionDefault());
            Assert.NotNull(MeilisearchJson.ReflectionDefault().Info<Movie>());
        }

        [Fact]
        public void ListInfoIsCachedPerElementType()
        {
            var json = TestJson.SourceGenJson;

            Assert.Same(json.ListInfo<Movie>(), json.ListInfo<Movie>());
            Assert.Equal("1", System.Text.Json.JsonSerializer.Deserialize("[{\"id\":\"1\"}]", json.ListInfo<Movie>()).Single().Id);
            Assert.Throws<NotSupportedException>(() => json.ListInfo<EnvelopeTests.UnregisteredDocument>());
        }

        [Fact]
        public void ItemsInfoIsCachedPerElementTypeAndNullHandling()
        {
            var json = TestJson.SourceGenJson;
            var movies = new[] { new Movie { Id = "1", Name = "Batman" } };

            Assert.Same(json.ItemsInfo<Movie>(), json.ItemsInfo<Movie>());
            Assert.Same(json.ItemsInfoRemoveNulls<Movie>(), json.ItemsInfoRemoveNulls<Movie>());
            Assert.NotSame(json.ItemsInfo<Movie>(), json.ItemsInfoRemoveNulls<Movie>());
            Assert.Equal("[{\"id\":\"1\",\"name\":\"Batman\",\"genre\":null}]", System.Text.Json.JsonSerializer.Serialize(movies, json.ItemsInfo<Movie>()));
            Assert.Equal("[{\"id\":\"1\",\"name\":\"Batman\"}]", System.Text.Json.JsonSerializer.Serialize(movies, json.ItemsInfoRemoveNulls<Movie>()));
            Assert.Throws<NotSupportedException>(() => json.ItemsInfo<EnvelopeTests.UnregisteredDocument>());
        }

        [Fact]
        public void CreateRequiresResolver()
        {
            Assert.Throws<ArgumentNullException>(() => MeilisearchJson.Create(null));
            Assert.Throws<ArgumentException>(() => MeilisearchJson.Create(new JsonSerializerOptions()));
        }

        [Fact]
        public void CreateCombinesCallerAndSdkMetadata()
        {
            var json = MeilisearchJson.Create(new JsonSerializerOptions { TypeInfoResolver = TestJsonContext.Default, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            Assert.NotNull(json.Info<Movie>());
            Assert.NotNull(json.Info<TaskInfo>());
            Assert.Equal(JsonNamingPolicy.CamelCase, json.WriteNulls.PropertyNamingPolicy);
            Assert.True(json.WriteNulls.PropertyNameCaseInsensitive);
            Assert.Equal(JsonIgnoreCondition.WhenWritingNull, json.RemoveNulls.DefaultIgnoreCondition);
            Assert.Equal(JsonIgnoreCondition.Never, json.WriteNulls.DefaultIgnoreCondition);
        }

        [Fact]
        public void RemoveNullsOmitsNullsAndWriteNullsKeepsThem()
        {
            var json = MeilisearchJson.Create(new JsonSerializerOptions { TypeInfoResolver = TestJsonContext.Default });
            var key = new Key { Name = "n" };

            Assert.DoesNotContain("\"description\"", JsonSerializer.Serialize(key, json.InfoRemoveNulls<Key>()));
            Assert.Contains("\"expiresAt\":null", JsonSerializer.Serialize(key, json.Info<Key>()));
        }

        [Fact]
        public void ClientAcceptsCallerOptions()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = TestJsonContext.Default };

            var client = new MeilisearchClient("http://localhost:7700", "key", options);

            Assert.Equal("key", client.ApiKey);
            Assert.Throws<ArgumentException>(() => new MeilisearchClient("http://localhost:7700", "key", new JsonSerializerOptions()));
        }
    }
}
