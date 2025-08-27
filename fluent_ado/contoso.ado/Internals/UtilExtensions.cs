using System.Diagnostics.CodeAnalysis;

namespace contoso.ado.Internals;

/// <summary>Internal fluent extensions to help with readability and simplicity.</summary>
internal static class UtilExtensions
{
    /// <summary>True if a collection is not null and has elements.</summary>
    public static bool IsNotNullOrEmpty<T>([NotNullWhen(true)] this IEnumerable<T>? source)
    {
        if (source is string str)
            return str.IsNotNullOrEmptyString();
        return (source != null) && (source.Any());
    }

    /// <summary>True if a collection is null or has no elements.</summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? source)
    {
        if (source is string str)
            return str.IsNullOrEmptyString();
        return (source == null) || (!source.Any());
    }

    /// <summary>True if the specified string is null, empty, or consists only of white-space characters.</summary>
    public static bool IsNullOrEmptyString([NotNullWhen(false)] this string? source) => string.IsNullOrWhiteSpace(source);

    /// <summary>True if a string is not null or empty or all blanks.</summary>
    public static bool IsNotNullOrEmptyString([NotNullWhen(true)] this string? source) => !string.IsNullOrWhiteSpace(source);

    /// <summary>
    /// If the supplied string is null, empty, or only spaces, returns null.
    /// Otherwise, returns the string with both leading and trailing white-space characters removed.
    /// </summary>
    /// <param name="source">The source string.</param>
    public static string? NullIfEmptyElseTrimmed(this string? source) => NullIfEmptyElseTrimmed(source, false);

    /// <summary>
    /// If the supplied string is null, empty, or only spaces, returns null.
    /// Otherwise, returns the string with trailing white-space characters removed,
    /// and also leading white-space removed when <paramref name="trimEndOnly"/> is false.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="trimEndOnly">True to trim only trailing white-space characters.</param>
    public static string? NullIfEmptyElseTrimmed(this string? source, bool trimEndOnly = false)
    {
        if (source == null)
            return source;
        if (string.IsNullOrWhiteSpace(source))
            return null;
        // framework will NOT allocate a new string if it does not have whitespace to trim
        return trimEndOnly ? source.TrimEnd() : source.Trim();
    }
}
