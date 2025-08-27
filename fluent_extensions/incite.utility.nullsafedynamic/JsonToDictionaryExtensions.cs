using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace contoso.utility.nullsafedynamic;

/// <summary>Extensions that parse json strings into a <see cref="Dictionary{String, Object}"/> or <see cref="List{Object}"/> with nested dictionaries or lists as needed.</summary>
public static class JsonToDictionaryExtensions
{
    readonly static ExpandoObjectConverter ExpandoConverter = new ExpandoObjectConverter();
    readonly static NullSafeDynamicConverter NullSafeDynamicConverter = new NullSafeDynamicConverter();

    // Goals of these extensions
    // - Accomodate dictionaries with complex nested types
    // - As a common library, vend .Net types (Dictionary, List) not Json.Net types (JObject, JArray)
    // - Make it easier to avoid RuntimeBinderExceptions and use dynamic objects with fluent syntax

    /// <summary>
    /// Uses Json.Net's ExpandoObjectConverter to parse a valid json string into an <see cref="ExpandoObject"/>
    /// whose members are recursively parsed into <see cref="ExpandoObject"/>, <see cref="List{ExpandoObject}"/> or primitives.
    /// Or parses the json as <see cref="List{ExpandoObject}"/> if the string represents an array
    /// </summary>
    /// <returns>
    /// The <see cref="ExpandoObject"/> result as its <see cref="IDictionary{String, Object}"/> interface
    /// or the <see cref="List{ExpandoObject}"/> result as a list of <see cref="IDictionary{String, Object}"/> interfaces
    /// or null if the string does not represent a json object.
    /// </returns>
    public static object ToDictionaryOrList(this string json)
    {
        if (json.IsBraceEnclosed())
            return json.ToExpandoDictionary();
        if (json.IsBracketEnclosed())
            return json.ToExpandoList();
        return new Dictionary<string, object?>(0);
    }

    /// <summary>
    /// Uses Json.Net's ExpandoObjectConverter to parse a valid json string into an <see cref="ExpandoObject"/>
    /// whose members are recursively parsed into <see cref="ExpandoObject"/>, <see cref="List{ExpandoObject}"/> or primitives.
    /// </summary>
    /// <returns>The <see cref="ExpandoObject"/> result as its <see cref="IDictionary{String, Object}"/> interface.</returns>
    public static IDictionary<string, object?> ToExpandoDictionary(this string json)
    {
        if (json.IsBraceEnclosed())
        {
            try
            {
                return JsonConvert.DeserializeObject<ExpandoObject>(json, ExpandoConverter) ?? new ExpandoObject();
            }
            catch { }
        }
        return new ExpandoObject();
    }

    /// <summary>
    /// Uses Json.Net's ExpandoObjectConverter to parse a valid json string into an <see cref="List{ExpandoObject}"/>
    /// and each item's members are recursively parsed into <see cref="ExpandoObject"/>, <see cref="List{ExpandoObject}"/> or primitives.
    /// </summary>
    /// <returns>The <see cref="List{ExpandoObject}"/> result as a list of <see cref="IDictionary{String, Object}"/> interfaces.</returns>
    public static List<IDictionary<string, object?>> ToExpandoList(this string json)
    {
        if (json.IsBracketEnclosed())
        {
            try
            {
                List<ExpandoObject> expandoList = JsonConvert.DeserializeObject<List<ExpandoObject>>(json, ExpandoConverter) ?? new List<ExpandoObject>(0);
                return expandoList.Cast<IDictionary<string, object?>>().ToList();
            }
            catch { }
        }

        return new List<ExpandoObject>(0).Cast<IDictionary<string, object?>>().ToList();
    }

    /// <summary>
    /// Uses a <see cref="NullSafeDynamicConverter"/> to parse a valid json string into an <see cref="NullSafeDynamic"/>
    /// whose members are recursively parsed into <see cref="NullSafeDynamic"/>, <see cref="List{NullSafeDynamic}"/> or primitives.
    /// </summary>
    /// <returns>The <see cref="NullSafeDynamic"/> result as its <see cref="IDictionary{String, Object}"/> interface.</returns>
    public static IDictionary<string, object?> ToNullSafeDynamic(this string json)
    {
        if (json.IsBraceEnclosed())
        {
            try
            {
                return JsonConvert.DeserializeObject<NullSafeDynamic>(json, NullSafeDynamicConverter) ?? new NullSafeDynamic();
            }
            catch { }
        }
        return new NullSafeDynamic();
    }

    /// <summary>
    /// Uses a <see cref="NullSafeDynamicConverter"/> to parse a valid json string into an <see cref="List{NullSafeDynamic}"/>
    /// and each item's members are recursively parsed into <see cref="NullSafeDynamic"/>, <see cref="List{NullSafeDynamic}"/> or primitives.
    /// </summary>
    /// <returns>The <see cref="List{NullSafeDynamic}"/> result as a list of <see cref="IDictionary{String, Object}"/> interfaces.</returns>
    public static List<IDictionary<string, object?>> ToNullSafeDynamicList(this string json)
    {
        if (json.IsBracketEnclosed())
        {
            try
            {
                List<NullSafeDynamic> dynamicObjList = JsonConvert.DeserializeObject<List<NullSafeDynamic>>(json, NullSafeDynamicConverter) ?? new List<NullSafeDynamic>(0);
                return dynamicObjList.Cast<IDictionary<string, object?>>().ToList();
            }
            catch { }
        }

        return new List<NullSafeDynamic>(0).Cast<IDictionary<string, object?>>().ToList();
    }

    /// <summary>Convert a boxed dictionary to a strongly typed instance of <typeparamref name="T"/>.</summary>
    public static T ToInstanceOf<T>(this IDictionary<string, object?> dictionary)
        where T : new()
    {
        try
        {
            return JObject.FromObject(dictionary).ToObject<T>() ?? new T();
        }
        catch
        {
            return new T();
        }
    }


    /// <summary>Convert a boxed list to a strongly typed List{T}.</summary>
    public static List<T> ToListOf<T>(this IEnumerable<object> src)
        where T : new()
    {
        try
        {
            return JArray.FromObject(src).ToObject<List<T>>() ?? new List<T>(0);
        }
        catch
        {
            return new List<T>(0);
        }
    }
}
