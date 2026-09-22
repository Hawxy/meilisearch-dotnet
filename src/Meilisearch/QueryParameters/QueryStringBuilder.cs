using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Meilisearch.QueryParameters
{
    /// <summary>
    /// Builds URL query strings from explicitly named values. Null values are skipped.
    /// </summary>
    internal sealed class QueryStringBuilder
    {
        private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";

        private readonly List<string> _pairs = new List<string>();

        public QueryStringBuilder Add(string key, string value)
        {
            if (value != null)
            {
                AddRaw(key, Uri.EscapeDataString(value));
            }

            return this;
        }

        public QueryStringBuilder Add(string key, int? value)
        {
            if (value.HasValue)
            {
                AddRaw(key, value.Value.ToString(CultureInfo.InvariantCulture));
            }

            return this;
        }

        public QueryStringBuilder Add(string key, DateTime? value)
        {
            if (value.HasValue)
            {
                AddRaw(key, Uri.EscapeDataString(value.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture)));
            }

            return this;
        }

        /// <summary>
        /// Adds a comma separated list. Items are not escaped, matching what the Meilisearch API expects.
        /// </summary>
        public QueryStringBuilder Add(string key, IEnumerable<string> values)
        {
            if (values != null)
            {
                AddRaw(key, string.Join(",", values));
            }

            return this;
        }

        public QueryStringBuilder Add(string key, IEnumerable<int> values)
        {
            if (values != null)
            {
                AddRaw(key, string.Join(",", values.Select(v => v.ToString(CultureInfo.InvariantCulture))));
            }

            return this;
        }

        public QueryStringBuilder Add<TEnum>(string key, IEnumerable<TEnum> values) where TEnum : struct, Enum
        {
            if (values != null)
            {
                AddRaw(key, string.Join(",", values.Select(v => v.ToString())));
            }

            return this;
        }

        /// <summary>
        /// Returns the query string, prefixed with <paramref name="uri"/> and a question mark when a URI is given.
        /// </summary>
        public string Build(string uri = "")
        {
            var queryString = string.Join("&", _pairs);
            if (string.IsNullOrWhiteSpace(uri))
            {
                return queryString;
            }

            return queryString.Length == 0 ? uri : $"{uri}?{queryString}";
        }

        private void AddRaw(string key, string encodedValue)
        {
            _pairs.Add($"{Uri.EscapeDataString(key)}={encodedValue}");
        }
    }
}
