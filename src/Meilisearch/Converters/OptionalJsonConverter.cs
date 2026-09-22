using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Meilisearch.Converters
{
    /// <summary>
    /// Class that helps to differ nullable and not provided values
    /// </summary>
    /// <typeparam name="T">Possible type of provided value</typeparam>
    public struct Optional<T> : IEquatable<Optional<T>>
    {
        /// <summary>
        /// Indicates whether a value was explicitly provided
        /// </summary>
        public bool HasValue { get; private set; }

        private T _value;

        /// <summary>
        /// Provided value
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws when no value has been provided</exception>
        public T Value
        {
            get =>
                HasValue
                    ? _value
                    : throw new InvalidOperationException("Value is not set");
            set
            {
                HasValue = true;
                _value = value;
            }
        }

        /// <summary>
        /// Instance with no value provided
        /// </summary>
        public static Optional<T> None => default;

        /// <summary>
        /// Implicitly converts a value into an <see cref="Optional{T}"/> instance
        /// </summary>
        /// <param name="value">Provided value</param>
        /// <returns>An <see cref="Optional{T}"/> containing the specified value</returns>
        public static implicit operator Optional<T>(T value) => new Optional<T> { Value = value };

        /// <inheritdoc/>
        public bool Equals(Optional<T> other)
            => HasValue == other.HasValue && (!HasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Optional<T> other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HasValue ? EqualityComparer<T>.Default.GetHashCode(_value) : 0;
    }

    /// <summary>
    /// Converter for <see cref="Optional{T}"/>. Apply it per property with
    /// <c>[JsonConverter(typeof(OptionalJsonConverter&lt;T&gt;))]</c> and
    /// <c>[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]</c> so unset values are omitted.
    /// </summary>
    /// <typeparam name="T">Possible type of provided value</typeparam>
    public class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
    {
        /// <inheritdoc/>
        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
        }
    }
}
