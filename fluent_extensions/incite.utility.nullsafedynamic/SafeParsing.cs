using System.Text;
using Newtonsoft.Json.Linq;

namespace contoso.utility.nullsafedynamic;

/// <summary>Helper extensions for managing json of unknown structure or validity.</summary>
public static class SafeParsing
{
    const char LeftBrace = '{';
    const char RightBrace = '}';
    const char LeftBracket = '[';
    const char RightBracket = ']';

    /// <summary>
    /// Attempts to parse <paramref name="strJson"/> into <paramref name="jToken"/>
    /// and returns a <see cref="JParseResult"/> indicating the outcome.
    /// </summary>
    public static JParseResult TryParseAsJson(this string strJson, out JToken? jToken)
    {
        jToken = null;
        JParseResult typeFlag = new JParseResult();

        if (string.IsNullOrWhiteSpace(strJson))
            return typeFlag;
        try
        {
            jToken = JToken.Parse(strJson);
            if (jToken is JObject)
                typeFlag.IsJObject = true;
            else if (jToken is JArray)
                typeFlag.IsJArray = true;
            else if (jToken is JValue)
                typeFlag.IsJValue = true;
            return typeFlag;
        }
        catch
        {
            return typeFlag;
        }
    }

    /// <summary>
    /// Attempts to parse <paramref name="strJson"/> as a valid json token (JObject, JArray, JValue)
    /// and returns either a <see cref="JToken"/> or the original string if it could not be parsed.
    /// </summary>
    public static object? SafeParse(this string strJson)
    {
        if (strJson.TryParseAsJson(out JToken? unknown).IsJson)
            return unknown;
        return strJson;
    }

    /// <summary>
    /// Attempts to parse <paramref name="strJson"/> as a valid json token (JObject, JArray, JValue)
    /// and returns either a <see cref="JToken"/> or the original string as a UTF-8 byte array encoded with Base-64, if it could not be parsed.
    /// </summary>
    public static object? SafeParseOrEncode(this string? strJson)
    {
        if (strJson == null)
            return strJson;

        if (strJson.TryParseAsJson(out JToken? unknown).IsJson)
            return unknown;
        else
        {
            try
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(strJson));
            }
            catch (Exception parseEx)
            {
                return JObject.FromObject(new { encodingError = parseEx.Message });
            }
        }
    }

    /// <summary>Return the first non-whitespace char, or a char with a value of zero.</summary>
    public static char GetFirstNonEmptyChar(this string strJson)
    {
        if (strJson == null)
            return char.MinValue;
        return strJson.FirstOrDefault(ch => !char.IsWhiteSpace(ch));
    }

    /// <summary>Return the last non-whitespace char, or a char with a value of zero.</summary>
    static char GetLastNonEmptyChar(this string strJson)
    {
        if (strJson == null)
            return char.MinValue;
        return strJson.Reverse().FirstOrDefault(ch => !char.IsWhiteSpace(ch));
    }

    /// <summary>True if <paramref name="strJson"/> starts and ends with braces, meaning it is likely a json object.</summary>
    public static bool IsBraceEnclosed(this string strJson)
    {
        return strJson != null
            && LeftBrace.Equals(strJson.GetFirstNonEmptyChar())
            && RightBrace.Equals(strJson.GetLastNonEmptyChar());
    }

    /// <summary>True if <paramref name="strJson"/> starts and ends with brackets, meaning it is likely a json array.</summary>
    public static bool IsBracketEnclosed(this string strJson)
    {
        return strJson != null
            && LeftBracket.Equals(strJson.GetFirstNonEmptyChar())
            && RightBracket.Equals(strJson.GetLastNonEmptyChar());
    }
}
