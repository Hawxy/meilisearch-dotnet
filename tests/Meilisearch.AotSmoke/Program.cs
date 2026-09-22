using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Meilisearch;
using Meilisearch.AotSmoke;
using Meilisearch.Converters;
using Meilisearch.QueryParameters;

// Exercises every wire path of the client from a Native AOT binary against a live Meilisearch.
// Exit code 0 means every step succeeded; anything else prints the failing step and exits 1.

var url = Environment.GetEnvironmentVariable("MEILISEARCH_URL") ?? "http://localhost:7700/";
var apiKey = Environment.GetEnvironmentVariable("MEILISEARCH_API_KEY") ?? "masterKey";
const string indexUid = "aot-smoke-movies";
const string swapUid = "aot-smoke-movies-swap";
const string vectorUid = "aot-smoke-vectors";

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = AppJsonContext.Default };
var client = new MeilisearchClient(url, apiKey, options);

try
{
    await Step("health", async () =>
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var health = await client.HealthAsync();
                Check(health.Status == "available", $"status {health.Status}");
                return;
            }
            catch (Exception) when (attempt < 30)
            {
                await Task.Delay(1000);
            }
        }
    });

    await Step("version and stats", async () =>
    {
        Check(!string.IsNullOrEmpty((await client.GetVersionAsync()).Version), "version missing");
        Check((await client.GetStatsAsync()) != null, "stats missing");
    });

    await Step("reset indexes", async () =>
    {
        foreach (var uid in new[] { indexUid, swapUid, vectorUid })
        {
            var existing = await client.GetAllIndexesAsync(new IndexesQuery { Limit = 100 });
            if (existing.Results.Any(i => i.Uid == uid))
            {
                await client.WaitForTaskAsync((await client.DeleteIndexAsync(uid)).TaskUid);
            }
        }
    });

    var index = client.Index(indexUid);

    await Step("create index", async () =>
    {
        var task = await Succeeded(client, client.CreateIndexAsync(indexUid, "id"));
        Check(task.IndexUid == indexUid, "indexUid mismatch");
        Check((await client.GetIndexAsync(indexUid)).PrimaryKey == "id", "primary key not set");
    });

    var movies = new List<Movie>
    {
        new Movie { Id = "1", Name = "Batman", Genre = "action", Year = 1989 },
        new Movie { Id = "2", Name = "Iron Man", Genre = "action", Year = 2008 },
        new Movie { Id = "3", Name = "Ant-Man", Genre = "comedy", Year = 2015 },
        new Movie { Id = "4", Name = "Rain Man", Genre = "drama" },
    };

    await Step("add and update documents", async () =>
    {
        await Succeeded(client, index.AddDocumentsAsync(movies.Take(2)));
        foreach (var task in await index.AddDocumentsInBatchesAsync(movies.Skip(2), 1))
        {
            await Succeeded(client, Task.FromResult(task));
        }

        Check((await index.GetStatsAsync()).NumberOfDocuments == 4, "documents not indexed");
        await Succeeded(client, index.UpdateDocumentsAsync(new[] { new Movie { Id = "4", Year = 1988 } }));
        var doc = await index.GetDocumentAsync<Movie>("4");
        Check(doc.Name == "Rain Man" && doc.Year == 1988, "update did not merge fields");
    });

    await Step("settings round trip", async () =>
    {
        await Succeeded(client, index.UpdateSettingsAsync(new Settings
        {
            FilterableAttributes = new FilterableAttribute[]
            {
                "genre",
                new FilterableAttribute
                {
                    AttributePatterns = new[] { "year" },
                    Features = new FilterableAttributeFeatures { FacetSearch = false, Filter = new FilterableAttributeFilterFeatures { Equality = true, Comparison = true } },
                },
            },
            SortableAttributes = new[] { "year" },
            Synonyms = new Dictionary<string, IEnumerable<string>> { ["movie"] = new[] { "film" } },
            TypoTolerance = new TypoTolerance { Enabled = true },
            Pagination = new Pagination { MaxTotalHits = 500 },
        }));

        var settings = await index.GetSettingsAsync();
        Check(settings.SortableAttributes.Contains("year"), "sortable attributes not applied");
        Check(settings.FilterableAttributes.Any(f => f.Attribute == "genre"), "string filterable attribute missing");
        Check(settings.FilterableAttributes.Any(f => f.AttributePatterns != null && f.AttributePatterns.Contains("year")), "object filterable attribute missing");
        Check(settings.Pagination.MaxTotalHits == 500, "pagination not applied");
        Check((await index.GetSynonymsAsync())["movie"].Contains("film"), "synonyms not applied");
    });

    await Step("search", async () =>
    {
        var plain = await index.SearchAsync<Movie>("man");
        Check(plain is SearchResult<Movie> result && result.Hits.Count >= 3 && result.EstimatedTotalHits >= 3, "plain search");

        var paginated = await index.SearchAsync<Movie>("man", new SearchQuery { HitsPerPage = 2, Page = 1 });
        Check(paginated is PaginatedSearchResult<Movie> page && page.Hits.Count == 2 && page.TotalPages >= 2, "paginated search");

        var filtered = await index.SearchAsync<Movie>("", new SearchQuery { Filter = "genre = action", Sort = new[] { "year:desc" }, Facets = new[] { "genre" } });
        Check(filtered.Hits.Select(m => m.Id).SequenceEqual(new[] { "2", "1" }), "string filter with sort");
        Check(filtered.FacetDistribution["genre"]["action"] == 2, "facet distribution");

        var arrayFilter = await index.SearchAsync<Movie>("", new SearchQuery { Filter = new[] { new[] { "genre = comedy", "genre = drama" } } });
        Check(arrayFilter.Hits.Count == 2, "array filter");

        var highlighted = await index.SearchAsync<Dictionary<string, object>>("bat", new SearchQuery { AttributesToHighlight = new[] { "name" }, ShowRankingScore = true });
        Check(highlighted.Hits.Single().ContainsKey("_formatted"), "untyped hit with _formatted");

        var facets = await index.FacetSearchAsync("genre", new FacetSearchQuery { FacetQuery = "act" });
        Check(facets.FacetHits.Single().Value == "action", "facet search");
    });

    await Step("get documents", async () =>
    {
        var all = await index.GetDocumentsAsync<Movie>(new DocumentsQuery { Limit = 10, Fields = new List<string> { "id", "name" } });
        Check(all.Results.Count() == 4 && all.Total == 4 && all.Results.All(m => m.Genre == null), "document listing with fields");

        var filtered = await index.GetDocumentsAsync<Movie>(new DocumentsQuery { Filter = "genre = comedy" });
        Check(filtered.Results.Single().Id == "3", "document fetch route with filter");

        var raw = await index.GetDocumentAsync<Dictionary<string, object>>("1");
        Check(((JsonElement)raw["name"]).GetString() == "Batman", "untyped document");
    });

    await Step("tasks", async () =>
    {
        var tasks = await client.GetTasksAsync(new TasksQuery
        {
            IndexUids = new List<string> { indexUid },
            Statuses = new List<TaskInfoStatus> { TaskInfoStatus.Succeeded },
            Types = new List<TaskInfoType> { TaskInfoType.DocumentAdditionOrUpdate },
            Limit = 5,
        });
        Check(tasks.Results.Any() && tasks.Results.All(t => t.Type == TaskInfoType.DocumentAdditionOrUpdate), "task query");
        var first = tasks.Results.First();
        Check(first.Details != null && first.Details.ContainsKey("receivedDocuments"), "task details");
        Check((await index.GetTasksAsync(new TasksQuery { Limit = 1 })).Results.Count() == 1, "index tasks");
    });

    string keyUid = null;
    await Step("keys", async () =>
    {
        var created = await client.CreateKeyAsync(new Key
        {
            Name = "aot-smoke",
            Description = "Search-only key for the AOT smoke test",
            Actions = new[] { KeyAction.Search },
            Indexes = new[] { indexUid },
            ExpiresAt = null,
        });
        keyUid = created.Uid;
        Check(created.KeyUid != null && created.Actions.Single() == KeyAction.Search, "key creation");
        Check((await client.GetKeysAsync(new KeysQuery { Limit = 100 })).Results.Any(k => k.Uid == keyUid), "key listing");
        var updated = await client.UpdateKeyAsync(keyUid, name: "aot-smoke-renamed");
        Check(updated.Name == "aot-smoke-renamed", "key update");

        var searchClient = new MeilisearchClient(url, created.KeyUid, options);
        Check((await searchClient.Index(indexUid).SearchAsync<Movie>("man")).Hits.Count >= 3, "search with restricted key");
    });

    await Step("tenant token", async () =>
    {
        var admin = await client.GetKeyAsync(keyUid);
        var rules = new TenantTokenRules(new Dictionary<string, object>
        {
            [indexUid] = new Dictionary<string, object> { ["filter"] = "genre = action" },
        });
        var token = client.GenerateTenantToken(admin.Uid, rules, admin.KeyUid, DateTime.UtcNow.AddMinutes(5));
        var tenantClient = new MeilisearchClient(url, token, options);
        Check((await tenantClient.Index(indexUid).SearchAsync<Movie>("")).Hits.Count == 2, "tenant token dictionary rules");

        var arrayToken = client.GenerateTenantToken(admin.Uid, new TenantTokenRules(new[] { indexUid }), admin.KeyUid, null);
        Check((await new MeilisearchClient(url, arrayToken, options).Index(indexUid).SearchAsync<Movie>("")).Hits.Count == 4, "tenant token array rules");
    });

    await Step("multi-search", async () =>
    {
        var multi = await client.MultiSearchAsync(new MultiSearchQuery
        {
            Queries = new List<SearchQuery>
            {
                new SearchQuery { IndexUid = indexUid, Q = "man", Limit = 2 },
                new SearchQuery { IndexUid = indexUid, Q = "", Filter = "genre = drama", HitsPerPage = 1, Page = 1 },
            },
        });
        Check(multi.Results.Count == 2, "multi-search count");
        Check(multi.Results[0] is SearchResult<JsonDocument> first && first.Hits.Count == 2 && first.IndexUid == indexUid, "multi-search plain result");
        Check(multi.Results[1] is PaginatedSearchResult<JsonDocument> second && second.Hits.Single().RootElement.GetProperty("id").GetString() == "4", "multi-search paginated result");

        var federated = await client.FederatedMultiSearchAsync<Movie>(new FederatedMultiSearchQuery
        {
            Queries = new List<FederatedSearchQuery>
            {
                new FederatedSearchQuery { IndexUid = indexUid, Q = "batman" },
                new FederatedSearchQuery { IndexUid = indexUid, Q = "iron" },
            },
            FederationOptions = new MultiSearchFederationOptions { Limit = 5 },
        });
        Check(federated.Hits.Count == 2, "federated search");

        var noOptions = await client.FederatedMultiSearchAsync<Movie>(new FederatedMultiSearchQuery
        {
            Queries = new List<FederatedSearchQuery> { new FederatedSearchQuery { IndexUid = indexUid, Q = "man" } },
        });
        Check(noOptions.Hits.Count >= 3, "federated search with empty federation object");
    });

    await Step("similar documents", async () =>
    {
        var vectorIndex = client.Index(vectorUid);
        await Succeeded(client, client.CreateIndexAsync(vectorUid, "id"));
        try
        {
            await Succeeded(client, vectorIndex.UpdateEmbeddersAsync(new Dictionary<string, Embedder>
            {
                ["manual"] = new Embedder { Source = EmbedderSource.UserProvided, Dimensions = 2 },
            }));
        }
        catch (MeilisearchApiError e)
        {
            Console.WriteLine($"  skipped: {e.Message}");
            return;
        }

        await Succeeded(client, vectorIndex.AddDocumentsAsync(new[]
        {
            new Dictionary<string, object> { ["id"] = "1", ["_vectors"] = new Dictionary<string, object> { ["manual"] = new[] { 1.0, 0.0 } } },
            new Dictionary<string, object> { ["id"] = "2", ["_vectors"] = new Dictionary<string, object> { ["manual"] = new[] { 0.9, 0.1 } } },
        }));
        var similar = await vectorIndex.SearchSimilarDocumentsAsync<Movie>(new SimilarDocumentsQuery("1") { Embedder = "manual" });
        Check(similar.Hits.First().Id == "2" && similar.Id == "1", "similar documents");
    });

    await Step("swap indexes", async () =>
    {
        await Succeeded(client, client.CreateIndexAsync(swapUid, "id"));
        await Succeeded(client, client.SwapIndexesAsync(new List<IndexSwap> { new IndexSwap(indexUid, swapUid) }));
        Check((await client.Index(swapUid).GetStatsAsync()).NumberOfDocuments == 4, "documents moved by swap");
        await Succeeded(client, client.SwapIndexesAsync(new List<IndexSwap> { new IndexSwap(indexUid, swapUid) }));
    });

    await Step("dynamic search rules", async () =>
    {
        try
        {
            if (!await client.EnableDynamicSearchRules())
            {
                Console.WriteLine("  skipped: feature not enabled");
                return;
            }

            var patch = new PatchDynamicSearchRule
            {
                Description = "aot-smoke",
                Active = (bool?)true,
                Precedence = (ulong?)1,
                Conditions = new DynamicSearchRuleConditions { Query = new QueryCondition { Words = "man" } },
                Actions = new[]
                {
                    new DSRAction { Selector = new DSRASelector { IndexUid = indexUid, Id = "3" }, Action = new PinAction { Position = 0 } },
                },
            };
            var task = await client.CreateOrUpdateDynamicSearchRuleAsync("aot-smoke-rule", patch);
            var done = await client.WaitForTaskAsync(task.TaskUid);
            if (done.Status != TaskInfoStatus.Succeeded)
            {
                Console.WriteLine($"  skipped: server rejected the rule ({done.Error?["message"]})");
                return;
            }

            var rule = await client.GetDynamicSearchRuleAsync("aot-smoke-rule");
            Check(rule.Actions.Single().Action is PinAction pin && pin.Position == 0, "pin action round trip");
            Check((await client.ListDynamicSearchRulesAsync()).Results.Any(r => r.Uid == "aot-smoke-rule"), "rule listing");
            await client.WaitForTaskAsync((await client.DeleteAllDynamicSearchRulesAsync()).TaskUid);
        }
        catch (MeilisearchApiError e)
        {
            Console.WriteLine($"  skipped: {e.Message}");
        }
    });

    await Step("error mapping", async () =>
    {
        try
        {
            await client.Index("aot-smoke-missing").GetStatsAsync();
            Check(false, "expected an API error");
        }
        catch (MeilisearchApiError e)
        {
            Check(e.Code == "index_not_found", $"unexpected error code {e.Code}");
        }
    });

    await Step("cleanup", async () =>
    {
        Check(await client.DeleteKeyAsync(keyUid), "key deletion");
        await Succeeded(client, client.DeleteIndexAsync(swapUid));
        await Succeeded(client, client.DeleteIndexAsync(vectorUid));
        await Succeeded(client, index.DeleteAllDocumentsAsync());
        await Succeeded(client, index.DeleteAsync());
    });
}
catch (Exception e)
{
    Console.Error.WriteLine($"FAILED: {e}");
    return 1;
}

Console.WriteLine("Native AOT smoke test passed.");
return 0;

static async Task Step(string name, Func<Task> action)
{
    Console.WriteLine($"[{name}]");
    await action();
}

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task<TaskResource> Succeeded(MeilisearchClient client, Task<TaskInfo> pending)
{
    var task = await client.WaitForTaskAsync((await pending).TaskUid, timeoutMs: 30000);
    if (task.Status != TaskInfoStatus.Succeeded)
    {
        throw new InvalidOperationException($"task {task.Uid} ({task.Type}) ended with {task.Status}: {task.Error?["message"]}");
    }

    return task;
}
