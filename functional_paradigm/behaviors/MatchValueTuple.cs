namespace contoso.functional.behaviors;

/// <summary></summary>
public static class MatchValueTuple
{
    /// <summary>Deconstruct a <see cref="ValueTuple"/> and map it's Item collection to <typeparamref name="TResult"/>.</summary>
    public static TResult Match<T1, T2, TResult>(this ValueTuple<T1, T2> tuple, Func<T1, T2, TResult> selector)
        => selector(tuple.Item1, tuple.Item2);

    /// <summary>Deconstruct a <see cref="ValueTuple"/> and map it's Item collection to <typeparamref name="TResult"/>.</summary>
    public static TResult Match<T1, T2, T3, TResult>(this ValueTuple<T1, T2, T3> tuple, Func<T1, T2, T3, TResult> selector)
        => selector(tuple.Item1, tuple.Item2, tuple.Item3);

    /// <summary>Deconstruct a <see cref="ValueTuple"/> and map it's Item collection to <typeparamref name="TResult"/>.</summary>
    public static TResult Match<T1, T2, T3, T4, TResult>(this ValueTuple<T1, T2, T3, T4> tuple, Func<T1, T2, T3, T4, TResult> selector)
        => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);

    /// <summary>Deconstruct a <see cref="ValueTuple"/> and map it's Item collection to <typeparamref name="TResult"/>.</summary>
    public static TResult Match<T1, T2, T3, T4, T5, TResult>(this ValueTuple<T1, T2, T3, T4, T5> tuple, Func<T1, T2, T3, T4, T5, TResult> selector)
        => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5);

    /// <summary>Deconstruct a <see cref="ValueTuple"/> and map it's Item collection to <typeparamref name="TResult"/>.</summary>
    public static TResult Match<T1, T2, T3, T4, T5, T6, TResult>(this ValueTuple<T1, T2, T3, T4, T5, T6> tuple, Func<T1, T2, T3, T4, T5, T6, TResult> selector)
        => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6);

    /// <summary>Deconstruct a <see cref="ValueTuple"/> and map it's Item collection to <typeparamref name="TResult"/>.</summary>
    public static TResult Match<T1, T2, T3, T4, T5, T6, T7, TResult>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7> tuple, Func<T1, T2, T3, T4, T5, T6, T7, TResult> selector)
        => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7);
}
