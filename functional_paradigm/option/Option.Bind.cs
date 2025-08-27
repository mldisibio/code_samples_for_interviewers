using static contoso.functional.FnConstructs;

namespace contoso.functional;

public static partial class Option
{
    /// <summary>
    /// Chain two the <c>Option</c> returning functions such that the inner value of <paramref name="src"/> is applied to <paramref name="next"/> if <paramref name="src"/> is Some,
    /// otherwise <paramref name="next"/> is not evaluated.
    /// </summary>
    /// <remarks>
    /// <c>Bind</c> flattens what would otherwise be a nested output container.
    /// <c>Bind</c> allows operations to be chained, evaluating the result of the previous before executing the next, 
    /// which supports the ROP concept of fail-fast where subsequent operations are not executed if the previous one fails.
    /// </remarks>
    public static Option<TOut> Bind<TIn, TOut>(this Option<TIn> src, Func<TIn, Option<TOut>> next)
        => src.Match
        (
            None: () => None,
            Some: (t) => next(t)
        );

    /// <summary>
    /// Treats <see cref="Option{TSource}"/> as a special case of an <see cref="IEnumerable{TIn}"/> that can be empty (None) or contain exactly one value (Some),
    /// allowing <see cref="Enumerable.SelectMany{TIn, TOut}(IEnumerable{TIn}, Func{TIn, IEnumerable{TOut}})"/> to be invoked by applying <paramref name="transformMany"/>.
    /// </summary>
    public static IEnumerable<TOut> TransformMany<TIn, TOut>(this Option<TIn> src, Func<TIn, IEnumerable<TOut>> transformMany)
        => src.AsEnumerable().SelectMany(transformMany);
}