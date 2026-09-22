using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Meilisearch.QueryParameters;

using Xunit;

namespace Meilisearch.Tests
{
    public class QueryStringTests
    {
        private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

        [Fact]
        public void BuilderEscapesStringValues()
        {
            var actual = new QueryStringBuilder().Add("fakeString", "+1").Build();

            Assert.Equal("fakeString=%2B1", actual);
        }

        [Fact]
        public void BuilderJoinsPairsWithAmpersand()
        {
            var actual = new QueryStringBuilder().Add("fakeString", "+1").Add("fakeInteger", 22).Build();

            Assert.Equal("fakeString=%2B1&fakeInteger=22", actual);
        }

        [Fact]
        public void BuilderFormatsDatesAndJoinsStringLists()
        {
            var date = DateTime.Now;
            var expectedDate = Uri.EscapeDataString(date.ToString(DateTimeFormat, CultureInfo.InvariantCulture));

            var actual = new QueryStringBuilder()
                .Add("fakeDate", date)
                .Add("fakeStringList", new List<string> { "hey", "ho" })
                .Build();

            Assert.Equal($"fakeDate={expectedDate}&fakeStringList=hey,ho", actual);
        }

        [Fact]
        public void BuilderJoinsEnumAndIntLists()
        {
            var actual = new QueryStringBuilder()
                .Add("statuses", new List<TaskInfoStatus> { TaskInfoStatus.Enqueued, TaskInfoStatus.Succeeded })
                .Add("types", new List<TaskInfoType> { TaskInfoType.IndexCreation, TaskInfoType.DocumentDeletion })
                .Add("uids", new List<int> { 1, 2 })
                .Build();

            Assert.Equal("statuses=Enqueued,Succeeded&types=IndexCreation,DocumentDeletion&uids=1,2", actual);
        }

        [Fact]
        public void BuilderSkipsNullValues()
        {
            var actual = new QueryStringBuilder()
                .Add("fakeString", (string)null)
                .Add("fakeInteger", (int?)null)
                .Add("fakeDate", (DateTime?)null)
                .Add("fakeList", (List<string>)null)
                .Build();

            Assert.Equal("", actual);
        }

        [Theory]
        [InlineData("simple")]
        [InlineData("com pl <->& ex")]
        public void BuilderPrependsUriAndEscapesPrimaryKey(string key)
        {
            var uri = "indexes/myindex/documents";

            var actual = new QueryStringBuilder().Add("primaryKey", key).Build(uri);

            Assert.Equal($"{uri}?primaryKey={Uri.EscapeDataString(key)}", actual);
        }

        [Fact]
        public void BuilderReturnsUriWhenEmpty()
        {
            var uri = "indexes/myindex/documents";

            Assert.Equal(uri, new QueryStringBuilder().Build(uri));
            Assert.Equal("", new QueryStringBuilder().Build());
        }

        [Fact]
        public void NullQueriesReturnUri()
        {
            var uri = "indexes/myindex/documents";

            Assert.Equal(uri, ((TasksQuery)null).ToQueryString(uri: uri));
            Assert.Equal(uri, ((CancelTasksQuery)null).ToQueryString(uri: uri));
            Assert.Equal(uri, ((DeleteTasksQuery)null).ToQueryString(uri: uri));
            Assert.Equal(uri, ((DocumentsQuery)null).ToQueryString(uri: uri));
            Assert.Equal(uri, ((IndexesQuery)null).ToQueryString(uri: uri));
            Assert.Equal(uri, ((KeysQuery)null).ToQueryString(uri: uri));
            Assert.Equal("", ((TasksQuery)null).ToQueryString());
        }

        [Fact]
        public void TasksQueryEmitsPropertiesInDeclarationOrder()
        {
            var date = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
            var expectedDate = Uri.EscapeDataString(date.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
            var query = new TasksQuery
            {
                Limit = 10,
                From = 5,
                IndexUids = new List<string> { "movies", "books" },
                Uids = new List<int> { 1, 2 },
                Statuses = new List<TaskInfoStatus> { TaskInfoStatus.Processing },
                Types = new List<TaskInfoType> { TaskInfoType.SettingsUpdate },
                CanceledBy = new List<string> { "3" },
                BeforeEnqueuedAt = date,
                AfterFinishedAt = date,
            };

            var actual = query.ToQueryString(uri: "tasks");

            Assert.Equal(
                "tasks?limit=10&from=5&indexUids=movies,books&uids=1,2&statuses=Processing&types=SettingsUpdate" +
                $"&canceledBy=3&beforeEnqueuedAt={expectedDate}&afterFinishedAt={expectedDate}",
                actual);
        }

        [Fact]
        public void CancelAndDeleteTasksQueriesEmitSameShape()
        {
            var cancel = new CancelTasksQuery { IndexUids = new List<string> { "movies" }, Uids = new List<int> { 7 } };
            var delete = new DeleteTasksQuery { IndexUids = new List<string> { "movies" }, Uids = new List<int> { 7 } };

            Assert.Equal("tasks/cancel?indexUids=movies&uids=7", cancel.ToQueryString(uri: "tasks/cancel"));
            Assert.Equal("tasks?indexUids=movies&uids=7", delete.ToQueryString(uri: "tasks"));
        }

        [Fact]
        public void IndexesAndKeysQueriesEmitLimitAndOffset()
        {
            Assert.Equal("indexes?limit=1&offset=2", new IndexesQuery { Limit = 1, Offset = 2 }.ToQueryString(uri: "indexes"));
            Assert.Equal("keys?offset=2", new KeysQuery { Offset = 2 }.ToQueryString(uri: "keys"));
        }

        [Fact]
        public void DocumentsQueryOmitsFilter()
        {
            var query = new DocumentsQuery { Limit = 1, Filter = "genre = action", Fields = new List<string> { "id", "title" } };

            Assert.Equal("indexes/myindex/documents?limit=1&fields=id,title", query.ToQueryString(uri: "indexes/myindex/documents"));
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(1, null, null)]
        [InlineData(null, 3, null)]
        [InlineData(null, null, new string[] { "attr" })]
        [InlineData(null, null, new string[] { "attr", "attr2", "attr3" })]
        [InlineData(1, 2, null)]
        [InlineData(1, null, new string[] { "attr" })]
        [InlineData(null, 2, new string[] { "attr" })]
        [InlineData(1, 2, new string[] { "attr" })]
        [InlineData(1, 2, new string[] { "attr", "attr2", "attr3" })]
        public void QueryStringsWithListAreEqualsForDocumentsQuery(int? offset, int? limit, string[] fields)
        {
            var uri = "indexes/myindex/documents";
            var dq = new DocumentsQuery { Offset = offset, Limit = limit, Fields = fields != null ? new List<string>(fields) : null };
            var actualQuery = dq.ToQueryString(uri: uri);

            Assert.NotEmpty(actualQuery);
            Assert.NotNull(actualQuery);
            if (limit != null)
            {
                Assert.Contains("limit", actualQuery);
                Assert.Contains(dq.Limit.ToString(), actualQuery);
            }
            if (offset != null)
            {
                Assert.Contains("offset", actualQuery);
                Assert.Contains(dq.Offset.ToString(), actualQuery);
            }
            if (fields != null)
            {
                Assert.Contains("fields", actualQuery);
                Assert.Contains(String.Join(",", dq.Fields), actualQuery);
            }
        }

        [Theory]
        [InlineData(null, new string[] { "id:asc", "title:desc" })]
        public void QueryStringsWithSortAreEqualForDocumentsQuery(int? limit, string[] sort)
        {
            var uri = "indexes/myindex/documents";
            var dq = new DocumentsQuery { Limit = limit, Sort = sort.ToList() };
            var actualQuery = dq.ToQueryString(uri: uri);

            Assert.NotEmpty(actualQuery);
            Assert.NotNull(actualQuery);
            Assert.Contains("sort", actualQuery);
            Assert.Contains(String.Join(",", dq.Sort), actualQuery);
        }
    }
}
