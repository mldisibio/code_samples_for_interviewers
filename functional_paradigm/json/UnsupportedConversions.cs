using System.Text.Json;
using System.Text.Json.Serialization;

namespace contoso.functional.json;

/// <summary>Provide json serializatin of <see cref="System.IntPtr"/> while not yet built-in to <see cref="System.Text.Json"/>.</summary>
public class IntPtrConverter : JsonConverter<IntPtr>
{
    /// <inheritdoc/>
    public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => IntPtr.Zero;

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options) => writer.WriteStringValue("System.IntPtr");
}

/// <summary>Provide json serializatin of <see cref="System.Type"/> while not yet built-in to <see cref="System.Text.Json"/>.</summary>
public class SystemTypeConverter : JsonConverter<Type>
{
    readonly static Type _objectType = typeof(object);
    /// <inheritdoc/>
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => _objectType;

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (value is not null)
            writer.WriteStringValue("System.Type");
    }
}

/// <summary>Provide json serializatin of <see cref="Exception"/> while not yet built-in to <see cref="System.Text.Json"/>.</summary>
public class ExceptionConverter : JsonConverter<Exception>
{
    /// <inheritdoc/>
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => default!;

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        if (value is not null)
            writer.WriteStringValue($"{value.GetType().FullName}: {value.Message}");
    }
}

