
using System.Diagnostics.CodeAnalysis;

namespace contoso.utility.fluentextensions;

/// <summary>Extensions enabling fluent syntax around enumerables.</summary>
public static class SequenceExtensions
{
    /// <summary>True if a collection is not null and has elements.</summary>
    public static bool IsNotNullOrEmpty<T>([NotNullWhen(true)]this IEnumerable<T>? source)
    {
        if (source is string str)
            return str.IsNotNullOrEmptyString();
        return (source != null) && (source.Any());
    }

    /// <summary>True if a collection is null or has no elements.</summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)]this IEnumerable<T>? source)
    {
        if (source is string str)
            return str.IsNullOrEmptyString();
        return (source == null) || (!source.Any());
    }

    /// <summary>
    /// Transforms the source collection of strings into a set of unique, non-empty, all upper case strings,
    /// or returns an empty set if <paramref name="source"/> is null.
    /// </summary>
    [return: NotNull]
    public static IEnumerable<string> AsSetOfUpperCaseStrings(this IEnumerable<string?>? source)
        => source == null ? Enumerable.Empty<string>() : source.Where(s => s.IsNotNullOrEmptyString()).Select(s => s!.ToUpper()).Distinct();

    /// <summary>
    /// Guarantees an non-null IEnumerable collection of <typeparamref name="T"/>
    /// Returns an empty IEnumerable <typeparamref name="T"/> if <paramref name="source"/> is null.
    /// </summary>
    [return: NotNull]
    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source) => source ?? Enumerable.Empty<T>();

    /// <summary>
    /// Guarantees an non-null <see cref="List{T}"/>.
    /// Returns an empty  <see cref="List{T}"/> if <paramref name="source"/> is null.
    /// </summary>
    [return: NotNull]
    public static List<T> EmptyIfNull<T>(this List<T>? source) => source ?? new List<T>(0);

    /// <summary>
    /// Guarantees an non-null Array of <typeparamref name="T"/>.
    /// Returns an empty Array of <typeparamref name="T"/> if <paramref name="source"/> is null.
    /// </summary>
    [return: NotNull]
    public static T[] EmptyIfNull<T>(this T[]? source) => source ?? (Array.Empty<T>());

    /// <summary>
    /// Transforms the source collection, where <typeparamref name="T"/> implements IEquatable{T}, into a set of unique, non-null values,
    /// or returns an empty set if <paramref name="source"/> is null.
    /// </summary>
    [return: NotNull]
    public static IEnumerable<T> AsNonNullSet<T>(this IEnumerable<T>? source) where T : IEquatable<T> 
        => source == null ? Enumerable.Empty<T>() : source.Where(item => item != null).Distinct();

    /// <summary>Convert a single item to a List, when required.</summary>
    [return: NotNull]
    public static List<T> AsListOfOne<T>(this T? source) => source == null ? new List<T>(0) : new List<T> { source };

    /// <summary>Concatenate any subcollection of items for flat reports. Returns an empty string if <paramref name="src"/> is null.</summary>
    /// <remarks>Each subcollection item ought to have a meaningful 'ToString()' override.</remarks>
    /// <param name="src">The list of items to transform into a delimited string.</param>
    /// <param name="delim">The delimiter. Can be multiple characters. Defaults to a comma without spaces.</param>
    /// <param name="preserveNulls">True to include null items as an empty string in the delimited list. False to remove them. Default is true.</param>
    [return: NotNull]
    public static string ToDelimitedString<T>(this IEnumerable<T>? src, string delim = ",", bool preserveNulls = true)
    {
        if (src == null)
            return string.Empty;

        delim ??= string.Empty;
        var items = src.Select(item => item?.ToString());
        if (preserveNulls)
            items = items.Select(val => val ?? String.Empty);
        else
            items = items.Where(val => val != null);
        
        return string.Join(delim, items);
    }

    /// <summary>Returns a sequence of batches of size <paramref name="batchSize"/> until there are no more elements.</summary>
    [return: NotNull]
    public static IEnumerable<IEnumerable<T>> TakeBy<T>(this IEnumerable<T>? src, int batchSize)
    {
        if (src == null)
            src = Enumerable.Empty<T>();
        batchSize = batchSize <= 0 ? 1 : batchSize;

        int pos = 0;
        int cnt = src.Count();
        while (pos < cnt)
        {
            yield return src.Skip(pos).Take(batchSize);
            pos += batchSize;
        }
    }

    /// <summary>
    /// True if <paramref name="source"/> is not null, <paramref name="value"/> is not null,
    /// and <paramref name="value"/> is found in <paramref name="source"/>. Will match an empty string, but not a null string.
    /// </summary>
    /// <param name="source">A collection of strings.</param>
    /// <param name="value">The string to verify is contained in <paramref name="source"/>.</param>
    /// <param name="caseSensitive">True to match as case-sensitive only. Default is false.</param>
    public static bool ContainsString(this IEnumerable<string>? source, string? value, bool caseSensitive = false)
    {
        if (source.IsNullOrEmpty())
            return false;
        if (value == null)
            return false;
        IEqualityComparer<string> comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        return source.Contains(value, comparer);
    }

}
