namespace contoso.functional;

/// <summary></summary>
public static partial class EnumerableContainer
{
    /// <summary>Pass thru to <see cref="Enumerable.SelectMany{TIn, TOut}(IEnumerable{TIn}, Func{TIn, IEnumerable{TOut}})"/>.</summary>
    public static IEnumerable<TOut> Bind<TIn, TOut>(this IEnumerable<TIn> src, Func<TIn, IEnumerable<TOut>> selector)
        => src.SelectMany(selector);

    /// <summary>
    /// Converts each element of <paramref name="src"/> to an <see cref="Option{TOut}"/>.
    /// By treating <see cref="Option{TOut}"/> as a special case of a list that can be empty (None) or contain exactly one value (Some),
    /// the result is a two dimensional list that when flattened by Bind, filters out all None values.
    /// </summary>
    public static IEnumerable<TResult> Bind<TIn, TResult>(this IEnumerable<TIn> src, Func<TIn, Option<TResult>> toOptionalResult)
        => src.Bind(t => toOptionalResult(t).AsEnumerable());
}