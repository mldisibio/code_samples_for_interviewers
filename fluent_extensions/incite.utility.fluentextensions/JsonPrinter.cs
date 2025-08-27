using System.Text.Json;
using System.Text.Json.Serialization;

namespace contoso.utility.fluentextensions;

/// <summary>Simple conversion to json with the built-in serializer.</summary>
public static class JsonPrinter
{
    const string _defaultJson = "{\"Object\":\"null\"}";
    readonly static JsonSerializerOptions _flatOpts = new JsonSerializerOptions{ ReferenceHandler = ReferenceHandler.IgnoreCycles };
    readonly static JsonSerializerOptions _indentedOpts = new JsonSerializerOptions{ ReferenceHandler = ReferenceHandler.IgnoreCycles, WriteIndented = true };


    /// <summary>Serialize <paramref name="src"/> to a formatted (indented) json string using the built-in serializer.</summary>
    public static string ToFormattedJson<T>(this T src)
    {
        if (src.IsNullOrDefault())
            return _defaultJson;

        try
        {
            return JsonSerializer.Serialize(src, _indentedOpts);
        }
        catch (Exception ex)
        {
            return $"{{\"SerializationError\":\"{ex.Message}\"}}";
        }
    }

    /// <summary>Serialize <paramref name="src"/> to a flattened json string using the built-in serializer.</summary>
    public static string ToFlatJson<T>(this T src)
    {
        if (src.IsNullOrDefault())
            return _defaultJson;

        try
        {
            return JsonSerializer.Serialize(src, _flatOpts);
        }
        catch (Exception ex)
        {
            return $"{{\"SerializationError\":\"{ex.Message}\"}}";
        }
    }
}
