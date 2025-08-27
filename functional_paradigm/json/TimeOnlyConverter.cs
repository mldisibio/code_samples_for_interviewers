using System.Text.Json;
using System.Text.Json.Serialization;

namespace contoso.functional.json;

/// <summary>Provide json serializatin of <see cref="TimeOnly"/> while not yet built-in to <see cref="System.Text.Json"/>.</summary>
/// <remarks>https://marcominerva.wordpress.com/2021/11/22/dateonly-and-timeonly-support-with-system-text-json/</remarks>
public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    readonly string _timeFormat;

    /// <summary>Will default to 'HH:mm:ss.fff' serialization.</summary>
    public TimeOnlyConverter() : this(null) { }

    /// <summary>Initialize with the time serialization format.</summary>
    public TimeOnlyConverter(string? timeFormat) => _timeFormat = timeFormat ?? "HH:mm:ss.fff";

    /// <inheritdoc/>
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return TimeOnly.TryParse(value!, out TimeOnly result) ? result : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(_timeFormat));
}