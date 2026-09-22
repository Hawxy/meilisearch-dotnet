using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Meilisearch.Converters;
using Meilisearch.QueryParameters;

using Xunit;

namespace Meilisearch.Tests
{
    public class ConvertersTests
    {
        private static readonly JsonSerializerOptions RemoveNulls = Constants.JsonSerializerOptionsRemoveNulls;
        private static readonly JsonSerializerOptions WriteNulls = Constants.JsonSerializerOptionsWriteNulls;

        [Fact]
        public void OptionalWritesOnlyProvidedValues()
        {
            var patch = new PatchDynamicSearchRule { Description = "desc", Precedence = (ulong?)null };

            var json = JsonSerializer.Serialize(patch, RemoveNulls);

            Assert.Equal("{\"description\":\"desc\",\"precedence\":null}", json);
        }

        [Fact]
        public void OptionalReadsProvidedValues()
        {
            var patch = JsonSerializer.Deserialize<PatchDynamicSearchRule>("{\"description\":\"desc\",\"active\":true}", WriteNulls);

            Assert.True(patch.Description.HasValue);
            Assert.Equal("desc", patch.Description.Value);
            Assert.True(patch.Active.HasValue);
            Assert.True(patch.Active.Value);
            Assert.False(patch.Actions.HasValue);
            Assert.False(patch.Conditions.HasValue);
        }

        [Fact]
        public void OptionalEqualityDistinguishesUnsetFromDefault()
        {
            Assert.Equal(Optional<bool?>.None, default(Optional<bool?>));
            Assert.NotEqual(Optional<bool?>.None, (Optional<bool?>)(bool?)null);
            Assert.Equal((Optional<string>)"a", (Optional<string>)"a");
        }

        [Fact]
        public void FederationOptionsAlwaysWritten()
        {
            var empty = new FederatedMultiSearchQuery { Queries = new List<FederatedSearchQuery>() };
            var withLimit = new FederatedMultiSearchQuery { Queries = new List<FederatedSearchQuery>(), FederationOptions = new MultiSearchFederationOptions { Limit = 5 } };

            Assert.Equal("{\"queries\":[],\"federation\":{}}", JsonSerializer.Serialize(empty, RemoveNulls));
            Assert.Equal("{\"queries\":[],\"federation\":{\"offset\":0,\"limit\":5}}", JsonSerializer.Serialize(withLimit, RemoveNulls));
        }

        [Fact]
        public void PinActionWritesDiscriminator()
        {
            var action = new DSRAction { Selector = new DSRASelector { IndexUid = "movies", Id = "1" }, Action = new PinAction { Position = 3 } };

            var json = JsonSerializer.Serialize(action, RemoveNulls);

            Assert.Equal("{\"selector\":{\"indexUid\":\"movies\",\"id\":\"1\"},\"action\":{\"type\":\"pin\",\"position\":3}}", json);
        }

        [Theory]
        [InlineData("{\"type\":\"pin\",\"position\":2}")]
        [InlineData("{\"position\":2,\"type\":\"pin\"}")]
        public void PinActionReadsDiscriminatorInAnyPosition(string json)
        {
            var action = JsonSerializer.Deserialize<BaseAction>(json, WriteNulls);

            var pin = Assert.IsType<PinAction>(action);
            Assert.Equal(2, pin.Position);
            Assert.Equal(ActionType.Pin, pin.Type);
        }

        [Fact]
        public void UnknownActionTypeThrows()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BaseAction>("{\"type\":\"boost\",\"position\":2}", WriteNulls));
        }

        public static IEnumerable<object[]> FilterValues()
        {
            yield return new object[] { "genre = action", "\"genre = action\"" };
            yield return new object[] { new[] { "a", "b" }, "[\"a\",\"b\"]" };
            yield return new object[] { new[] { new[] { "a", "b" }, new[] { "c" } }, "[[\"a\",\"b\"],[\"c\"]]" };
            yield return new object[] { new List<string> { "a" }.Select(x => x + "!"), "[\"a!\"]" };
            yield return new object[] { new Dictionary<string, object> { ["k"] = 1, ["n"] = new Dictionary<string, string> { ["x"] = "y" } }, "{\"k\":1,\"n\":{\"x\":\"y\"}}" };
            yield return new object[] { JsonDocument.Parse("{\"a\":[1,2.5,true,null]}").RootElement, "{\"a\":[1,2.5,true,null]}" };
            yield return new object[] { 42L, "42" };
            yield return new object[] { false, "false" };
        }

        [Theory]
        [MemberData(nameof(FilterValues))]
        public void UntypedFilterValuesSerialize(object filter, string expected)
        {
            var json = JsonSerializer.Serialize(new DocumentsQuery { Filter = filter }, RemoveNulls);

            Assert.Equal($"{{\"filter\":{expected}}}", json);
        }

        [Fact]
        public void UntypedRejectsUnsupportedTypes()
        {
            var query = new DocumentsQuery { Filter = new Movie { Id = "1" } };

            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(query, RemoveNulls));
        }

        [Fact]
        public void UntypedDictionariesReadAsJsonElements()
        {
            var json = "{\"taskUid\":1,\"indexUid\":\"movies\",\"status\":\"succeeded\",\"type\":\"documentAdditionOrUpdate\"," +
                       "\"details\":{\"receivedDocuments\":3,\"nested\":{\"a\":\"b\"}},\"enqueuedAt\":\"2024-01-01T00:00:00Z\"}";

            var task = JsonSerializer.Deserialize<TaskInfo>(json, WriteNulls);

            Assert.Equal(TaskInfoStatus.Succeeded, task.Status);
            Assert.Equal(TaskInfoType.DocumentAdditionOrUpdate, task.Type);
            Assert.Equal(3, Assert.IsType<JsonElement>(task.Details["receivedDocuments"]).GetInt32());
            Assert.Equal("b", Assert.IsType<JsonElement>(task.Details["nested"]).GetProperty("a").GetString());
        }

        [Fact]
        public void EmbedderRequestTemplateRoundTrips()
        {
            var embedder = new Embedder
            {
                Source = EmbedderSource.Rest,
                Request = new Dictionary<string, object> { ["model"] = "m", ["input"] = new[] { "{{text}}", "{{..}}" } },
            };

            var json = JsonSerializer.Serialize(embedder, RemoveNulls);
            var back = JsonSerializer.Deserialize<Embedder>(json, WriteNulls);

            Assert.Contains("\"request\":{\"model\":\"m\",\"input\":[\"{{text}}\",\"{{..}}\"]}", json);
            Assert.Equal("m", Assert.IsType<JsonElement>(back.Request["model"]).GetString());
        }

        [Fact]
        public void TenantTokenRulesSerializeWithoutReflection()
        {
            var dictionary = new TenantTokenRules(new Dictionary<string, object> { ["*"] = new Dictionary<string, object> { ["filter"] = "tag = Tale" } });
            var array = new TenantTokenRules(new[] { "books" });

            Assert.Equal("{\"*\":{\"filter\":\"tag = Tale\"}}", dictionary.ToJson());
            Assert.Equal("[\"books\"]", array.ToJson());
        }

        [Fact]
        public void KeyActionsRoundTripDotCase()
        {
            var key = new Key { Actions = new[] { KeyAction.DocumentsAdd, KeyAction.All, KeyAction.IndexesGet } };

            var json = JsonSerializer.Serialize(key, RemoveNulls);
            var back = JsonSerializer.Deserialize<Key>(json, WriteNulls);

            Assert.Contains("\"actions\":[\"documents.add\",\"*\",\"indexes.get\"]", json);
            Assert.Equal(key.Actions, back.Actions);
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Key>("{\"actions\":[\"documents.explode\"]}", WriteNulls));
        }

        [Fact]
        public void IndexAndTasksResultsUseExplicitPropertyNames()
        {
            var index = JsonSerializer.Deserialize<Index>("{\"uid\":\"movies\",\"primaryKey\":\"id\",\"createdAt\":\"2024-01-01T00:00:00Z\"}", WriteNulls);
            var tasks = JsonSerializer.Deserialize<TasksResults<IEnumerable<TaskResource>>>("{\"results\":[],\"limit\":20,\"from\":5,\"next\":null,\"total\":7}", WriteNulls);

            Assert.Equal("movies", index.Uid);
            Assert.Equal("id", index.PrimaryKey);
            Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), index.CreatedAt);
            Assert.Equal(5, tasks.From);
            Assert.Null(tasks.Next);
            Assert.Equal(7, tasks.Total);
            Assert.Equal("{\"uid\":\"movies\",\"primaryKey\":\"id\",\"createdAt\":\"2024-01-01T00:00:00+00:00\"}", JsonSerializer.Serialize(index, RemoveNulls));
        }
    }
}
