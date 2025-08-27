using Unit = System.ValueTuple;

namespace contoso.functional;

/// <summary></summary>
public static partial class Option
{
    /// <summary>
    /// Execute <paramref name="action"/> expected to have a side-effect, on <paramref name="src"/>.
    /// Returns an <see cref="Option{Unit}"/> instead of 'void'
    /// </summary>
    /// <remarks>Keep the scope of <paramref name="action"/> as small as possible and move 'ForEach' as far to the end of chained functions as possible.</remarks>
    public static Option<Unit> ForEach<T>(this Option<T> src, Action<T> action)
        => Map(src, action.ToFunc());

    /// <summary>
    /// Execute <paramref name="action"/> if <paramref name="src"/> represents a 'Some' of <typeparamref name="T"/>, otherwise do nothing.
    /// Returns <paramref name="src"/> as is.
    /// </summary>
    public static Option<T> Do<T>(this Option<T> src, Action<T> action)
    {
        src.ForEach(action);
        return src;
    }
}