using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static contoso.functional.FnConstructs;

namespace contoso.functional.json;

/// <summary>Json converter for the <see cref="Option{T}"/> type.</summary>
/// <remarks>As provided by <see href="https://github.com/la-yumba/functional-csharp-code-2/blob/master/LaYumba.Functional/Serialization/Json.cs"/>.</remarks>
public class OptionConverter : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Option<>);

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
        => Activator.CreateInstance
           (
              typeof(OptionConverterInner<>).MakeGenericType(new Type[] { type.GetGenericArguments()[0] }),
              BindingFlags.Instance | BindingFlags.Public,
              binder: null,
              args: [options],
              culture: null
           ) as JsonConverter;

    private class OptionConverterInner<T> : JsonConverter<Option<T>>
    {
        readonly JsonConverter<T> _valueConverter;
        readonly Type _valueType;

        public OptionConverterInner(JsonSerializerOptions options)
        {
            // For performance, use the existing converter if available
            _valueConverter = (JsonConverter<T>)options.GetConverter(typeof(T));
            // Cache the value type
            _valueType = typeof(T);
        }

        /// <inheritdoc/>
        public override bool HandleNull => true;

        /// <inheritdoc/>
        public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // deserialize 'null' into a None
            if (reader.TokenType == JsonTokenType.Null)
                return None;

            // deserialize non-null value into a Some
            T? t = _valueConverter != null
                   ? _valueConverter.Read(ref reader, _valueType, options)
                   : JsonSerializer.Deserialize<T>(ref reader, options);

            return Some(t ?? throw new InvalidOperationException($"'{t}' could not be deserialized into a {typeof(T)}"));
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Option<T> option, JsonSerializerOptions options)
        => option.Match
        (
           None: () => writer.WriteNullValue(),
           Some: (value) =>
           {
               if (_valueConverter != null)
                   _valueConverter.Write(writer, value, options);
               else
                   JsonSerializer.Serialize(writer, value, options);
           }
        );
    }
}
