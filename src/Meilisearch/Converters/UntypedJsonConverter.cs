using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Meilisearch.Converters
{
    /// <summary>
    /// Serializes values held in <see cref="object"/>-typed members without reflection over their runtime type.
    /// Supports null, strings, booleans, numbers, dates, GUIDs, <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// <see cref="JsonNode"/>, string-keyed dictionaries and any other enumerable. Reads produce <see cref="JsonElement"/>.
    /// </summary>
    public class UntypedJsonConverter : JsonConverter<object>
    {
        /// <inheritdoc/>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadValue(ref reader);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            => WriteValue(writer, value, options);

        internal static object ReadValue(ref Utf8JsonReader reader)
            => reader.TokenType == JsonTokenType.Null ? null : JsonElement.ParseValue(ref reader);

        internal static Dictionary<string, object> ReadDictionary(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected an object but found {reader.TokenType}.");
            }

            var result = new Dictionary<string, object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return result;
                }

                var key = reader.GetString();
                reader.Read();
                result[key] = ReadValue(ref reader);
            }

            throw new JsonException("Unexpected end of JSON while reading an object.");
        }

        internal static void WriteValue(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case string s:
                    writer.WriteStringValue(s);
                    return;
                case bool b:
                    writer.WriteBooleanValue(b);
                    return;
                case int i:
                    writer.WriteNumberValue(i);
                    return;
                case long l:
                    writer.WriteNumberValue(l);
                    return;
                case double d:
                    writer.WriteNumberValue(d);
                    return;
                case float f:
                    writer.WriteNumberValue(f);
                    return;
                case decimal m:
                    writer.WriteNumberValue(m);
                    return;
                case uint ui:
                    writer.WriteNumberValue(ui);
                    return;
                case ulong ul:
                    writer.WriteNumberValue(ul);
                    return;
                case short sh:
                    writer.WriteNumberValue(sh);
                    return;
                case ushort ush:
                    writer.WriteNumberValue(ush);
                    return;
                case byte by:
                    writer.WriteNumberValue(by);
                    return;
                case sbyte sb:
                    writer.WriteNumberValue(sb);
                    return;
                case DateTime dt:
                    writer.WriteStringValue(dt);
                    return;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto);
                    return;
                case Guid g:
                    writer.WriteStringValue(g);
                    return;
                case JsonElement element:
                    element.WriteTo(writer);
                    return;
                case JsonDocument document:
                    document.RootElement.WriteTo(writer);
                    return;
                case JsonNode node:
                    node.WriteTo(writer, options);
                    return;
                case IDictionary dictionary:
                    WriteDictionary(writer, dictionary, options);
                    return;
                case IEnumerable<KeyValuePair<string, object>> pairs:
                    WriteDictionary(writer, pairs, options);
                    return;
                case IEnumerable enumerable:
                    writer.WriteStartArray();
                    foreach (var item in enumerable)
                    {
                        WriteValue(writer, item, options);
                    }

                    writer.WriteEndArray();
                    return;
                default:
                    throw new JsonException(
                        $"Cannot serialize a value of type '{value.GetType()}' in an untyped position. " +
                        "Use primitives, strings, JsonElement, JsonNode, string-keyed dictionaries or collections of those.");
            }
        }

        internal static void WriteDictionary(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object>> pairs, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var pair in pairs)
            {
                writer.WritePropertyName(pair.Key);
                WriteValue(writer, pair.Value, options);
            }

            writer.WriteEndObject();
        }

        private static void WriteDictionary(Utf8JsonWriter writer, IDictionary dictionary, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WritePropertyName(Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture));
                WriteValue(writer, entry.Value, options);
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// <see cref="UntypedJsonConverter"/> for <see cref="Dictionary{TKey, TValue}"/> members with <see cref="object"/> values.
    /// </summary>
    public class UntypedDictionaryJsonConverter : JsonConverter<Dictionary<string, object>>
    {
        /// <inheritdoc/>
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => UntypedJsonConverter.ReadDictionary(ref reader);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
            => UntypedJsonConverter.WriteDictionary(writer, value, options);
    }

    /// <summary>
    /// <see cref="UntypedJsonConverter"/> for <see cref="IReadOnlyDictionary{TKey, TValue}"/> members with <see cref="object"/> values.
    /// </summary>
    public class UntypedReadOnlyDictionaryJsonConverter : JsonConverter<IReadOnlyDictionary<string, object>>
    {
        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => UntypedJsonConverter.ReadDictionary(ref reader);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, object> value, JsonSerializerOptions options)
            => UntypedJsonConverter.WriteDictionary(writer, value, options);
    }
}
