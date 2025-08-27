using static contoso.functional.FnConstructs;
using Unit = System.ValueTuple;

namespace contoso.functional;

/// <summary></summary>
public static partial class Adapters
{
    /// <summary>Revert to function with no input.</summary>
    public static Func<T> ToNullary<T>(this Func<Unit, T> fn)
        => () => fn(Unit());

    /// <summary>
    /// Return a reduced function that yields <typeparamref name="TResult"/> from the <paramref name="producer"/>'s input of <typeparamref name="T1"/> 
    /// by applying the intermediate <paramref name="consumer"/> to <paramref name="producer"/>.
    /// </summary>
    public static Func<T1, TResult> ReducedFrom<T1, T2, TResult>(this Func<T2, TResult> consumer, Func<T1, T2> producer)
       => t1 => consumer(producer(t1));

    /// <summary>
    /// Return a reduced function that yields <typeparamref name="TResult"/> from the <paramref name="producer"/>'s input of <typeparamref name="T1"/> 
    /// by applying the intermediate <paramref name="consumer"/> to <paramref name="producer"/>.
    /// </summary>
    public static Func<T1, TResult> Map<T1, T2, TResult>(this Func<T1, T2> producer, Func<T2, TResult> consumer)
       => (t1) => consumer(producer(t1));

    /// <summary>
    /// Return a reduced function that yields <typeparamref name="TResult"/> from the <paramref name="producer"/>'s input tuple of <typeparamref name="T1"/> and <typeparamref name="T2"/> 
    /// by applyingthe intermediate <paramref name="consumer"/> to <paramref name="producer"/>.
    /// </summary>
    public static Func<T1, T2, TResult> Map<T1, T2, T3, TResult>(this Func<T1, T2, T3> producer, Func<T3, TResult> consumer)
       => (t1, t2) => consumer(producer(t1, t2));

    /// <summary></summary>
    public static Func<T, bool> Negate<T>(this Func<T, bool> predicate) => t => !predicate(t);

}
