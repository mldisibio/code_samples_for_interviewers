using static contoso.functional.FnConstructs;

namespace contoso.functional;

#pragma warning disable CS1591
public static partial class Option
{
    /// <summary>Alternative syntax to return Some <typeparamref name="T"/> or an eagerly evaluated <paramref name="default"/> if None.</summary>
    public static T GetValueOr<T>(this Option<T> src, T @default)
        => src.Match
        (
            None: () => @default,
            Some: t => t
        );

    /// <summary>Alternative syntax to return Some <typeparamref name="T"/> or lazily evaluate <paramref name="factory"/> if None.</summary>
    public static T GetValueOr<T>(this Option<T> src, Func<T> factory)
        => src.Match
        (
            None: factory,
            Some: t => t
        );

    public static Task<T> GetOrElse<T>(this Option<T> opt, Func<Task<T>> fallback)
        => opt.Match
        (
          () => fallback(),
          (t) => Async(t)
        );

    /// <summary>Return <paramref name="src"/> if Some, otherwise <paramref name="otherwise"/>.</summary>
    public static Option<T> IfSomeElse<T>(this Option<T> src, Option<T> otherwise)
        => src.Match
        (
            None: () => otherwise,
            Some: _ => src
        );

    /// <summary>Return <paramref name="src"/> if Some, otherwise invoke <paramref name="otherwise"/>.</summary>
    public static Option<T> IfSomeElse<T>(this Option<T> src, Func<Option<T>> otherwise)
        => src.Match
        (
            None: () => otherwise(),
            Some: _ => src
        );
}
#pragma warning restore CS1591
