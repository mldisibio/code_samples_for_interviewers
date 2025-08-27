using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace contoso.utility.fluentextensions;

/// <summary>Fluent extensions over one-off types or task..</summary>
public static class SpecialtyExtensions
{
    readonly static JsonSerializerOptions _simpleOpts = new JsonSerializerOptions{ ReferenceHandler = ReferenceHandler.IgnoreCycles };
    
    // https://stackoverflow.com/a/864860/458354
    /// <summary>True if reference or nullable instance is null or if value instance is default.</summary>
    public static bool IsNullOrDefault<T>([NotNullWhen(false)] this T src) => EqualityComparer<T>.Default.Equals(src, default(T));

    /// <summary>
    /// Format a <see cref="Type"/>name and expand any generic parameters for display,
    /// in particular for logging or diagnostics.
    /// </summary>
    [return: NotNull]
    public static string FormatTypeName(this Type? t)
    {
        string tName = t?.Name ?? string.Empty;
        try
        {
            if (t?.IsGenericType == true)
            {
                Type[] genericArgs = t.GetGenericArguments();
                if (genericArgs.Any())
                {
                    string baseType = tName.Split('`').FirstOrDefault() ?? tName;
                    string typeArgs = String.Join(",", genericArgs.Select(arg => arg.FormatTypeName()));
                    return $"{baseType}<{typeArgs}>";
                }
            }
            return tName;
        }
        catch
        {
            return tName;
        }
    }

    /// <summary>Format <paramref name="ex"/> as a short message when handling expected exceptions.</summary>
    [return: NotNull]
    public static string AsShortMessage([AllowNull] this Exception ex) => ex == null ? string.Empty : $"[{ex.GetType().Name}]: {ex.Message}";

    /// <summary>Convert a simple POCO into a simple string/object dictionary.</summary>
    [return: NotNull]
    public static IDictionary<string, object?> ToSimpleDictionary<T>(this T src)
    {
        Dictionary<string, object?>? simple = null;
        if (!IsNullOrDefault(src))
        {
            try
            {
                simple = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(src, _simpleOpts), _simpleOpts);
            }
            catch { }
        }
        return simple ?? new Dictionary<string, object?>(0);
    }

}
