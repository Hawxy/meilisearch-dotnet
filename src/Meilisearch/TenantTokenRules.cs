using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;

using Meilisearch.Converters;

namespace Meilisearch
{
    /// <summary>
    /// Wrapper class used to map all the supported types to be used in
    /// the `searchRules` claim in the Tenant Tokens.
    /// </summary>
    public class TenantTokenRules
    {
        private readonly object _rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantTokenRules"/> class based on a rules json object.        
        /// </summary>
        /// <param name="rules">
        /// 
        /// example:
        /// 
        /// {'*': {"filter": 'tag = Tale'}}
        /// 
        /// </param>
        public TenantTokenRules(IReadOnlyDictionary<string, object> rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantTokenRules"/> class based on a rules string array.        
        /// </summary>
        /// <param name="rules">
        /// 
        /// example:
        /// 
        /// ['books']
        /// 
        /// </param>
        public TenantTokenRules(string[] rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Accessor method used to retrieve the searchRules claim.
        /// </summary>
        /// <returns>A object with the supported type representing the `searchRules`.</returns>
        public object ToClaim()
        {
            return _rules;
        }

        /// <summary>
        /// Serializes the rules to JSON without reflection over their runtime types.
        /// </summary>
        internal string ToJson() => ToJsonElement().GetRawText();

        /// <summary>
        /// Serializes the rules to a detached <see cref="JsonElement"/> without reflection over their runtime types.
        /// </summary>
        internal JsonElement ToJsonElement()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                UntypedJsonConverter.WriteValue(writer, _rules, null);
            }

            using (var document = JsonDocument.Parse(buffer.WrittenMemory))
            {
                return document.RootElement.Clone();
            }
        }
    }
}
