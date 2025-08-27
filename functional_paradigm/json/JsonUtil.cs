using System.Text.Json;
using System.Text.Json.Serialization;

namespace contoso.functional.json;

/// <summary>Extension methods to convert any object to a json string with user-friendly formatting options.</summary>
public static class JsonUtil
{
    readonly static JsonSerializerOptions _flatOps;
    readonly static JsonSerializerOptions _indentedOps;
    const string _defaultJson = "{\"Object\":\"null\"}";

    static JsonUtil()
    {

        _flatOps = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles, IncludeFields = false };
        _indentedOps = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles, IncludeFields = false, WriteIndented = true };

        new List<JsonConverter>
        {
            new DateOnlyConverter("yyyy-MM-dd"),
            new TimeOnlyConverter("HH:mm:ss"),
            new IntPtrConverter(),
            new SystemTypeConverter(),
            new ExceptionConverter(),
            new OptionConverter()
        }
        .ForEach(c =>
        {
            _flatOps.Converters.Add(c);
            _indentedOps.Converters.Add(c);
        });
    }

    /// <summary>Serialize any given object to a formatted (indented) json string.</summary>
    public static string ToIndentedJson<T>(this T src)
    {
        if (src is null)
            return _defaultJson;
        try
        {
            return JsonSerializer.Serialize(src, _indentedOps);
        }
        catch (Exception ex)
        {
            return $"{{\"SerializationError\":\"{ex.Message}\"}}";
        }
    }

    /// <summary>Serialize any given object to an unformatted (flattened) json string.</summary>
    public static string ToFlatJson<T>(this T src)
    {
        if (src is null)
            return _defaultJson;
        try
        {
            return JsonSerializer.Serialize(src, _flatOps);
        }
        catch (Exception ex)
        {
            return $"{{\"SerializationError\":\"{ex.Message}\"}}";
        }
    }
}
