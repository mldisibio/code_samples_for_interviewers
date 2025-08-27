using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace contoso.sqlite.raw.pgwriter
{
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

        /// <summary>
        /// If the supplied string is null, empty, or only spaces, returns <c>String.Empty</c>.
        /// Otherwise, returns the string with trailing white-space characters removed,
        /// and also leading white-space removed when <paramref name="trimEndOnly"/> is false.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="trimEndOnly">True to trim only trailing white-space characters.</param>
        [return: NotNull]
        public static string EmptyIfNullElseTrimmed(this string? source, bool trimEndOnly = false)
        {
            if (string.Equals(source, String.Empty))
                return source!;

            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;
            // framework will NOT allocate a new string if it does not have whitespace to trim
            return trimEndOnly ? source.TrimEnd() : source.Trim();
        }

        /// <summary>Shortcut to <c>String.Equals(source, other, StringComparison.Ordinal)</c> returning true if arguments are equal, case and whitespace sensitive.</summary>
        /// <remarks>Does not modify either string at all. Returns true if both are null.</remarks>
        public static bool IsExactlyEqualTo(this string? source, string? other) => string.Equals(source, other, StringComparison.Ordinal);

        /// <summary>Shortcut to <c>String.Equals(source.Trim(), other.Trim(), StringComparison.OrdinalIgnoreCase))</c> - case and trailing whitespace insensitive.</summary>
        /// <remarks>Does not modify either string at all. Returns true if both are null but false if only one is null, even if other is empty.</remarks>
        public static bool IsAcceptedlyEqualTo(this string? source, string? other)
        {
            if (source == null || other == null)
                return source == null && other == null;
            if (string.Equals(source, other))
                return true;
            // compare as if both strings are trimmed, without actually allocating new strings
            return source.AsSpan().Trim().CompareTo(other.AsSpan().Trim(), StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
