namespace contoso.functional;

/// <summary></summary>
public static class StringExtensions
{
    /// <summary>Elide <paramref name="src"/> to <paramref name="n"/> characters.</summary>
    public static string Elided(this string? src, int n)
    {
        if (string.IsNullOrEmpty(src))
            return string.Empty;

        var trimmedInput = src.Trim();

        return trimmedInput.Length <= n
               ? trimmedInput
               : string.Concat
                 (
                    trimmedInput.AsSpan(0, n),
                    "...",
                    trimmedInput[0].Equals('{') ? "}" : trimmedInput[0].Equals('[') ? "]" : trimmedInput[0].Equals('(') ? ")" : string.Empty
                 );
    }

    /// <summary>Replace <paramref name="sep"/> with a blank space.</summary>
    public static string ReplaceWithSpace(this string s, string sep) => string.IsNullOrEmpty(s) ? s : s.Replace(sep, " ");

    /// <summary>Convert a nullable string into an <see cref="Option{String}"/>.</summary>
    public static Option<string> Maybe(this string? s) => s == null ? FnConstructs.None : FnConstructs.Some(s);

    /// <summary>Return <see cref="String.Empty"/> for an <see cref="Option{String}"/> of None, otherwise its value.</summary>
    public static string EmptyIfNone(this Option<string> src) => src.GetValueOr(string.Empty);

    /// <summary>Return null for an <see cref="Option{String}"/> of None, otherwise its value.</summary>
    public static string? NullIfNone(this Option<string> src) => src.Match<string?>(None: () => null, Some: s => s);

}
