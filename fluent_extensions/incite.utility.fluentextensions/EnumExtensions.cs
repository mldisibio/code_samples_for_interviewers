namespace contoso.utility.fluentextensions;

/// <summary>Extensions enabling fluent syntax around enums.</summary>
public static class EnumExtensions
{
    /// <summary>
    /// Attempts to converts <paramref name="sourceEnum"/> to <typeparamref name="TOut"/> 
    /// if <typeparamref name="TOut"/> defines an equivalently named enumeration, case-insensitive.
    /// </summary>
    public static TOut? ToSynonym<TIn, TOut>(this TIn sourceEnum)
        where TIn : struct, System.Enum
        where TOut : struct, System.Enum
        => ToEnum<TOut>(sourceEnum.ToString("G"));

    /// <summary>
    /// Attempt to convert <paramref name="source"/> to <typeparamref name="TEnum"/> 
    /// if <typeparamref name="TEnum"/> defines an equivalently named enumeration, case-insensitive.
    /// </summary>
    public static TEnum? ToEnum<TEnum>(this string source) where TEnum : struct, System.Enum
    {
        if (!string.IsNullOrEmpty(source))
        {
            if (Enum.TryParse(source, ignoreCase: true, out TEnum result))
            {
                if (Enum.IsDefined(typeof(TEnum), result))
                    return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns an enum of type <typeparamref name="TEnum"/> 
    /// if <typeparamref name="TEnum"/> defines an equivalently named constant,
    /// otherwise returns the default value as specified
    /// </summary>
    public static TEnum ToEnumOrDefault<TEnum>(this string source, TEnum defaultValue) where TEnum : struct, System.Enum 
        => ToEnum<TEnum>(source) ?? defaultValue;
}
