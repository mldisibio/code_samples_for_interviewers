
using Newtonsoft.Json;

namespace contoso.utility.nullsafedynamic;
/// <summary>Extension methods to convert any object to a json string with user-friendly formatting options.</summary>
public static class JsonPrinter
{
    readonly static JsonSerializerSettings _jsonLoggingSetting = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

    /// <summary>Serialize any given object to a formatted (indented) json string.</summary>
    public static string ToJsonString<T>(this T src)
    {
        if (src != null)
        {
            try
            {
                return JsonConvert.SerializeObject(src, Formatting.Indented, _jsonLoggingSetting);
            }
            catch (Exception ex)
            {
                return $"{{\"SerializationError\":\"{ex.Message}\"}}";
            }
        }
        else
            return "{\"Object\":\"null\"}";
    }

    /// <summary>Serialize any given object to an unformatted (flattened) json string.</summary>
    public static string ToJsonStringFlat<T>(this T src)
    {
        if (src != null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(src, Formatting.None, _jsonLoggingSetting);
                if (json != null && json.IndexOf('\n') >= 0)
                    return json.Replace("\n", " ").Replace("\r", string.Empty);
                else
                    return json ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"{{\"SerializationError\":\"{ex.Message}\"}}";
            }
        }
        else
            return "{\"Object\":\"null\"}";
    }

    /// <summary>Print a dictionary of values with the keys right aligned.</summary>
    public static string ToAlignedDictionary<T>(this Dictionary<string, T> methodData)
    {
        try
        {
            if (methodData != null && methodData.Any())
            {
                int keyLenMax = methodData.Keys.Where(k => !string.IsNullOrEmpty(k)).Max(k => k.Length);
                string keyFormat = $"{{0,{keyLenMax}}}";
                var alignedDictionary = methodData.ToDictionary(kvp => string.Format(keyFormat, kvp.Key ?? string.Empty), kvp => kvp.Value?.ToString());
                return alignedDictionary.ToJsonString();
            }
        }
        catch (Exception ex)
        {
            return $"{{\"SerializationError\":\"{ex.Message}\"}}";
        }
        return "{\"Dictionary\":\"null\"}";
    }
}
