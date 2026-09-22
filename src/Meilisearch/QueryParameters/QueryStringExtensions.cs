namespace Meilisearch.QueryParameters
{
    /// <summary>
    /// Converts query parameter objects into URL query strings. A null query yields <c>uri</c> unchanged.
    /// </summary>
    internal static class QueryStringExtensions
    {
        internal static string ToQueryString(this TasksQuery query, string uri = "")
        {
            var builder = new QueryStringBuilder();
            if (query != null)
            {
                builder
                    .Add("limit", query.Limit)
                    .Add("from", query.From)
                    .Add("indexUids", query.IndexUids)
                    .Add("uids", query.Uids)
                    .Add("statuses", query.Statuses)
                    .Add("types", query.Types)
                    .Add("canceledBy", query.CanceledBy)
                    .Add("beforeEnqueuedAt", query.BeforeEnqueuedAt)
                    .Add("afterEnqueuedAt", query.AfterEnqueuedAt)
                    .Add("beforeStartedAt", query.BeforeStartedAt)
                    .Add("afterStartedAt", query.AfterStartedAt)
                    .Add("beforeFinishedAt", query.BeforeFinishedAt)
                    .Add("afterFinishedAt", query.AfterFinishedAt);
            }

            return builder.Build(uri);
        }

        internal static string ToQueryString(this CancelTasksQuery query, string uri = "")
        {
            var builder = new QueryStringBuilder();
            if (query != null)
            {
                builder
                    .Add("indexUids", query.IndexUids)
                    .Add("statuses", query.Statuses)
                    .Add("types", query.Types)
                    .Add("uids", query.Uids)
                    .Add("canceledBy", query.CanceledBy)
                    .Add("beforeEnqueuedAt", query.BeforeEnqueuedAt)
                    .Add("afterEnqueuedAt", query.AfterEnqueuedAt)
                    .Add("beforeStartedAt", query.BeforeStartedAt)
                    .Add("afterStartedAt", query.AfterStartedAt)
                    .Add("beforeFinishedAt", query.BeforeFinishedAt)
                    .Add("afterFinishedAt", query.AfterFinishedAt);
            }

            return builder.Build(uri);
        }

        internal static string ToQueryString(this DeleteTasksQuery query, string uri = "")
        {
            var builder = new QueryStringBuilder();
            if (query != null)
            {
                builder
                    .Add("indexUids", query.IndexUids)
                    .Add("statuses", query.Statuses)
                    .Add("types", query.Types)
                    .Add("uids", query.Uids)
                    .Add("canceledBy", query.CanceledBy)
                    .Add("beforeEnqueuedAt", query.BeforeEnqueuedAt)
                    .Add("afterEnqueuedAt", query.AfterEnqueuedAt)
                    .Add("beforeStartedAt", query.BeforeStartedAt)
                    .Add("afterStartedAt", query.AfterStartedAt)
                    .Add("beforeFinishedAt", query.BeforeFinishedAt)
                    .Add("afterFinishedAt", query.AfterFinishedAt);
            }

            return builder.Build(uri);
        }

        /// <summary>
        /// Builds the GET query string. <see cref="DocumentsQuery.Filter"/> is not included because filtered
        /// requests are sent as a JSON body to the fetch route instead.
        /// </summary>
        internal static string ToQueryString(this DocumentsQuery query, string uri = "")
        {
            var builder = new QueryStringBuilder();
            if (query != null)
            {
                builder
                    .Add("limit", query.Limit)
                    .Add("offset", query.Offset)
                    .Add("fields", query.Fields)
                    .Add("sort", query.Sort);
            }

            return builder.Build(uri);
        }

        internal static string ToQueryString(this IndexesQuery query, string uri = "")
        {
            var builder = new QueryStringBuilder();
            if (query != null)
            {
                builder
                    .Add("limit", query.Limit)
                    .Add("offset", query.Offset);
            }

            return builder.Build(uri);
        }

        internal static string ToQueryString(this KeysQuery query, string uri = "")
        {
            var builder = new QueryStringBuilder();
            if (query != null)
            {
                builder
                    .Add("limit", query.Limit)
                    .Add("offset", query.Offset);
            }

            return builder.Build(uri);
        }
    }
}
