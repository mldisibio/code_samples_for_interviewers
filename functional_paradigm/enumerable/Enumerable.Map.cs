namespace contoso.functional.advanced;

#pragma warning disable CS1591
public static partial class EnumerableContainerAdvanced
{
    /// <summary>Pass-thru to <see cref="Enumerable.Select{TIn, TOut}(IEnumerable{TIn}, Func{TIn, TOut})"/></summary>
    public static IEnumerable<TOut> Map<TIn, TOut>(this IEnumerable<TIn> src, Func<TIn, TOut> map)
        => src.Select(map);

    /// <summary>Return a collection of unary functions expecting <typeparamref name="T2"/> each having an item from <paramref name="src"/> partially applied as the first argument to <paramref name="producer"/>.</summary>
    public static IEnumerable<Func<T2, TResult>> Map<T1, T2, TResult>(this IEnumerable<T1> src, Func<T1, T2, TResult> producer)
        => src.Map(producer.Curry());

    public static IEnumerable<Func<T2, Func<T3, TResult>>> Map<T1, T2, T3, TResult>(this IEnumerable<T1> src, Func<T1, T2, T3, TResult> producer)
        => src.Map(producer.Curry());
}
#pragma warning restore CS1591
