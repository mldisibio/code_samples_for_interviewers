namespace contoso.utility.nullsafedynamic;
/// <summary>Shortcut to parsed json type.</summary>
public struct JParseResult
{
    /// <summary>True if the parsed json is a JObject.</summary>
    public bool IsJObject { get; internal set; }
    /// <summary>True if the parsed json is a JArray.</summary>
    public bool IsJArray { get; internal set; }
    /// <summary>True if the parsed json is a JValue.</summary>
    public bool IsJValue { get; internal set; }
    /// <summary>True if the parsed json is a JObject or JArray.</summary>
    public bool IsObjectOrArray { get { return IsJObject || IsJArray; } }
    /// <summary>True if the json parsed successfully as JObject, JArray or JValue.</summary>
    public bool IsJson { get { return IsJObject || IsJArray || IsJValue; } }
    /// <summary>True if the json could not be parsed as JObject, JArray or JValue.</summary>
    public bool IsNotJson { get { return !IsJson; } }
}
