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
    [JsonSerializable(typeof(Movie))]
    internal partial class TestDocumentsContext : JsonSerializerContext
    {
    }

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
        public void CreateRequiresResolver()
        {
            Assert.Throws<ArgumentNullException>(() => MeilisearchJson.Create(null));
            Assert.Throws<ArgumentException>(() => MeilisearchJson.Create(new JsonSerializerOptions()));
        }

        [Fact]
        public void CreateCombinesCallerAndSdkMetadata()
        {
            var json = MeilisearchJson.Create(new JsonSerializerOptions { TypeInfoResolver = TestDocumentsContext.Default, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

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
            var json = MeilisearchJson.Create(new JsonSerializerOptions { TypeInfoResolver = TestDocumentsContext.Default });
            var key = new Key { Name = "n" };

            Assert.DoesNotContain("\"description\"", JsonSerializer.Serialize(key, json.InfoRemoveNulls<Key>()));
            Assert.Contains("\"expiresAt\":null", JsonSerializer.Serialize(key, json.Info<Key>()));
        }

        [Fact]
        public void ClientAcceptsCallerOptions()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = TestDocumentsContext.Default };

            var client = new MeilisearchClient("http://localhost:7700", "key", options);

            Assert.Equal("key", client.ApiKey);
            Assert.Throws<ArgumentException>(() => new MeilisearchClient("http://localhost:7700", "key", new JsonSerializerOptions()));
        }
    }
}
