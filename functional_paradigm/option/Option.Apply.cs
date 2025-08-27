using static contoso.functional.FnConstructs;

namespace contoso.functional.advanced;

/// <summary></summary>
public static partial class OptionAdvanced
{
    /// <summary>'Apply' the inner value of <paramref name="optionT1"/> as the first argument to the unary function contained in <paramref name="optionFn"/>.</summary>
    /// <remarks>
    /// When using <c>Apply</c>, each chained function is evaluated. Allows accumulation of errors from multiple operations, for example
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;Func&lt;Cat, string&gt;&gt; getName = Some&lt;Func&lt;Cat, string&gt;&gt;(cat =&gt; $"Meet {cat.Name}, aged {cat.Age}!");
    /// Option&lt;Cat&gt; maybeCat = Some&lt;Cat&gt;(new (Name: "Fluffy", Age: 5));
    /// Option&lt;string&gt; greeting = getName.Apply(maybeCat); // greeting contains "Meet Fluffy, aged 5!"
    /// </code>
    /// </example>
    public static Option<TResult> Apply<T1, TResult>(this Option<Func<T1, TResult>> optionFn, Option<T1> optionT1)
        => optionFn.Match
        (
            None: () => None,
            Some: (fn) => optionT1.Match
            (
                None: () => None,
                Some: (val) => Some(fn(val))
            )
        );

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, TResult>> Apply<T1, T2, TResult>(this Option<Func<T1, T2, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.Curry), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, TResult>> Apply<T1, T2, T3, TResult>(this Option<Func<T1, T2, T3, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, T4, TResult>> Apply<T1, T2, T3, T4, TResult>(this Option<Func<T1, T2, T3, T4, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, T4, T5, TResult>> Apply<T1, T2, T3, T4, T5, TResult>(this Option<Func<T1, T2, T3, T4, T5, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, T4, T5, T6, TResult>> Apply<T1, T2, T3, T4, T5, T6, TResult>(this Option<Func<T1, T2, T3, T4, T5, T6, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, T4, T5, T6, T7, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, TResult>(this Option<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, T4, T5, T6, T7, T8, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Option<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);

    /// <summary>Return the curried function expecting the remaining arguments after the inner value of <paramref name="optionT1"/> has been partially applied as the first argument to the function contained in <paramref name="optionFn"/>..</summary>
    public static Option<Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Option<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> optionFn, Option<T1> optionT1)
       => Apply(optionFn.Map(FnConstructs.CurryFirst), optionT1);
}