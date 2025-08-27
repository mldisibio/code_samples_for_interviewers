using System.Diagnostics.CodeAnalysis;

namespace contoso.sqlite.raw
{

    internal static class UtilExtensions
    {
        static readonly SQLitePCL.sqlite3? _nullDbHandle = default;
        static readonly SQLitePCL.sqlite3_stmt? _nullStmtHandle = default;

        /// <summary>Returns a sequence of batches of size <paramref name="batchSize"/> until there are no more elements.</summary>
        public static IEnumerable<IEnumerable<T>> TakeBy<T>(this IEnumerable<T>? src, int batchSize)
        {
            src ??= Enumerable.Empty<T>();
            int idx = 0;
            int cnt = src.Count();
            while (idx < cnt)
            {
                yield return src.Skip(idx).Take(batchSize);
                idx += batchSize;
            }
        }

        /// <summary>Validate that the SafeHandle around the native sqlite database handle still points to a resource.</summary>
        public static bool SeemsValid(this SQLitePCL.sqlite3? dbHandle)
        {
            return !(dbHandle == null || dbHandle == _nullDbHandle || dbHandle.IsInvalid || dbHandle.IsClosed);
        }

        /// <summary>Validate that the SafeHandle around the native sqlite statement handle still points to a resource.</summary>
        public static bool SeemsValid(this SQLitePCL.sqlite3_stmt? stmtHandle)
        {
            return !(stmtHandle == null || stmtHandle == _nullStmtHandle || stmtHandle.IsInvalid || stmtHandle.IsClosed);
        }

        /// <summary>Throw if the SafeHandle around the native sqlite database handle no longer points to a resource.</summary>
        public static void ThrowIfInvalid(this SQLitePCL.sqlite3? dbHandle, string? filePath = null)
        {
            if (!dbHandle.SeemsValid())
                throw new SqliteDbException(ResultCodes.InvalidHandle, "SQLitePCL.sqlite3 is invalid", filePath);
        }

        /// <summary>Throw if the SafeHandle around the native sqlite statement handle no longer points to a resource.</summary>
        public static void ThrowIfInvalid(this SQLitePCL.sqlite3_stmt? stmtHandle, string? filePath = null)
        {
            if (!stmtHandle.SeemsValid())
                throw new SqliteDbException(ResultCodes.InvalidHandle, "SQLitePCL.sqlite3_stmt is invalid", filePath);
        }

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
            if (string.Equals(source, string.Empty))
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

        /// <summary>Format an elapsed millisecond value for display.</summary>
        public static string Display(this long? ms)
        {
            if (ms.HasValue)
            {
                var timeSpan = TimeSpan.FromMilliseconds(ms.Value);
                return timeSpan.TotalMilliseconds < 60000 ? $"{timeSpan:ss\\.fff}" : timeSpan.TotalMilliseconds < 86400000 ? $"{timeSpan:hh\\:mm\\:ss\\.fff}" : $"{timeSpan:d\\.hh\\:mm\\:ss\\.fff}";
            }
            return string.Empty;
        }
    }
}
