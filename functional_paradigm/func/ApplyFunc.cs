namespace contoso.functional;
#pragma warning disable CS1591

public static class ApplyFunc
{
    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, TResult> Apply<T1, T2, TResult>(this Func<T1, T2, TResult> fn, T1 t1)
        => t2 => fn(t1, t2);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, TResult> Apply<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> fn, T1 t1)
        => (t2, t3) => fn(t1, t2, t3);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> fn, T1 t1)
        => (t2, t3, t4) => fn(t1, t2, t3, t4);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> fn, T1 t1)
        => (t2, t3, t4, t5) => fn(t1, t2, t3, t4, t5);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, T4, T5, T6, TResult> Apply<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> fn, T1 t1)
        => (t2, t3, t4, t5, t6) => fn(t1, t2, t3, t4, t5, t6);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, T4, T5, T6, T7, TResult> Apply<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> fn, T1 t1)
        => (t2, t3, t4, t5, t6, t7) => fn(t1, t2, t3, t4, t5, t6, t7);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, T4, T5, T6, T7, T8, TResult> Apply<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> fn, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8) => fn(t1, t2, t3, t4, t5, t6, t7, t8);

    /// <summary>Return a function expecting the remaining arguments after <paramref name="t1"/> has been partially applied.</summary>
    public static Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> fn, T1 t1)
        => (t2, t3, t4, t5, t6, t7, t8, t9) => fn(t1, t2, t3, t4, t5, t6, t7, t8, t9);

}
#pragma warning restore CS1591
