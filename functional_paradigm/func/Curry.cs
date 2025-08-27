namespace contoso.functional;

#pragma warning disable CS1591

public static partial class FnConstructs
{
    /// <summary>Curry a function</summary>
    /// <remarks>
    /// Given a function 'add' that takes 'x' and 'y', return a function that takes 'x' and will then return a function that takes 'y' and will add 'x' to it.
    /// </remarks>
    /// <example>
    /// <code>
    /// var add = (int x, int y) => x + y;
    /// var addThree = add.Curry()(3);
    /// int result = addThree(70); // result = 12
    /// </code>
    /// </example>
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(this Func<T1, T2, TResult> fn)
        => t1 => t2 => fn(t1, t2);

    /// <summary>Curry a function</summary>
    /// <example>
    /// <code>
    /// var add = (int x, int y, int z) => x + y + z;
    /// var addSeven = add.Curry()(3)(4);
    /// int result = addSeven(70); // result = 77
    /// </code>
    /// </example>
    public static Func<T1, Func<T2, Func<T3, TResult>>> Curry<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> fn)
        => t1 => t2 => t3 => fn(t1, t2, t3);

    public static Func<T1, Func<T2, T3, TResult>> CurryFirst<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> fn)
        => t1 => (t2, t3) => fn(t1, t2, t3);

    /// <summary>Curry a function</summary>
    /// <example>
    /// <code>
    /// var add = (int x, int y, int z, int i) => x + y + z + i;
    /// var addTwelve = add.CurryFirst()(3).Curry()(4)(5);
    /// int result = addTwelve(70); // result = 82
    /// </code>
    /// </example>
    public static Func<T1, Func<T2, T3, T4, TResult>> CurryFirst<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> fn)
        => t1 => (t2, t3, t4) => fn(t1, t2, t3, t4);

    public static Func<T1, Func<T2, T3, T4, T5, TResult>> CurryFirst<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> fn)
        => t1 => (t2, t3, t4, t5) => fn(t1, t2, t3, t4, t5);

    /// <summary>Curry a function</summary>
    /// <example>
    /// <code>
    /// var add = (int x, int y, int z, int i, int j, int k) => x + y + z + i + j + k;
    /// var addTwentyFive = add.CurryFirst()(3).CurryFirst()(4).CurryFirst()(5).Curry()(6)(7);
    /// int result = addTwentyFive(70); // result = 95
    /// </code>
    /// </example>
    public static Func<T1, Func<T2, T3, T4, T5, T6, TResult>> CurryFirst<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> fn)
        => t1 => (t2, t3, t4, t5, t6) => fn(t1, t2, t3, t4, t5, t6);

    public static Func<T1, Func<T2, T3, T4, T5, T6, T7, TResult>> CurryFirst<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> fn)
        => t1 => (t2, t3, t4, t5, t6, t7) => fn(t1, t2, t3, t4, t5, t6, t7);

    public static Func<T1, Func<T2, T3, T4, T5, T6, T7, T8, TResult>> CurryFirst<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> fn)
        => t1 => (t2, t3, t4, t5, t6, t7, t8) => fn(t1, t2, t3, t4, t5, t6, t7, t8);

    public static Func<T1, Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult>> CurryFirst<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> fn)
        => t1 => (t2, t3, t4, t5, t6, t7, t8, t9) => fn(t1, t2, t3, t4, t5, t6, t7, t8, t9);
}
#pragma warning restore CS1591