using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using Xunit;

namespace Meilisearch.Tests
{
    public class TenantTokenPayloadTests
    {
        private const string Uid = "6062abda-a5aa-4414-ac91-ecd7944c0f8d";
        private const string ApiKey = "masterKeyOfAtLeastSixteenCharacters";

        private static JsonElement Payload(string token)
        {
            var segment = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            segment = segment.PadRight(segment.Length + (4 - segment.Length % 4) % 4, '=');
            return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(segment))).RootElement;
        }

        [Fact]
        public void DictionaryRulesAreEmbeddedAsJsonObject()
        {
            var rules = new TenantTokenRules(new Dictionary<string, object>
            {
                ["*"] = new Dictionary<string, object> { ["filter"] = "tag = Tale" },
                ["books"] = null,
            });

            var payload = Payload(TenantToken.GenerateToken(Uid, rules, ApiKey, null));

            Assert.Equal(Uid, payload.GetProperty("apiKeyUid").GetString());
            Assert.Equal(JsonValueKind.Object, payload.GetProperty("searchRules").ValueKind);
            Assert.Equal("tag = Tale", payload.GetProperty("searchRules").GetProperty("*").GetProperty("filter").GetString());
            Assert.Equal(JsonValueKind.Null, payload.GetProperty("searchRules").GetProperty("books").ValueKind);
            Assert.False(payload.TryGetProperty("exp", out _));
        }

        [Fact]
        public void ArrayRulesAreEmbeddedAsJsonArray()
        {
            var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var payload = Payload(TenantToken.GenerateToken(Uid, new TenantTokenRules(new[] { "books", "movies" }), ApiKey, expiresAt));

            Assert.Equal(JsonValueKind.Array, payload.GetProperty("searchRules").ValueKind);
            Assert.Equal("movies", payload.GetProperty("searchRules")[1].GetString());
            Assert.Equal(new DateTimeOffset(expiresAt).ToUnixTimeSeconds(), payload.GetProperty("exp").GetInt64());
        }

        [Fact]
        public void TokenUsesHs256()
        {
            var header = Payload("." + TenantToken.GenerateToken(Uid, new TenantTokenRules(new[] { "*" }), ApiKey, null).Split('.')[0]);

            Assert.Equal("HS256", header.GetProperty("alg").GetString());
            Assert.Equal("JWT", header.GetProperty("typ").GetString());
        }
    }
}
