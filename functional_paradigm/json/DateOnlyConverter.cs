using System.Text.Json;
using System.Text.Json.Serialization;

namespace contoso.functional.json;

/// <summary>Provide json serializatin of <see cref="DateOnly"/> while not yet built-in to <see cref="System.Text.Json"/>.</summary>
/// <remarks>https://marcominerva.wordpress.com/2021/11/22/dateonly-and-timeonly-support-with-system-text-json/</remarks>
public class DateOnlyConverter : JsonConverter<DateOnly>
{
    readonly string _dateFormat;

    /// <summary>Will default to 'yyyy-MM-dd' serialization.</summary>
    public DateOnlyConverter() : this(null) { }

    /// <summary>Initialize with the date serialization format.</summary>
    public DateOnlyConverter(string? dateFormat) => _dateFormat = dateFormat ?? "yyyy-MM-dd";

    /// <inheritdoc/>
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        return DateOnly.TryParse(value!, out DateOnly result) ? result : default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(_dateFormat));
}
