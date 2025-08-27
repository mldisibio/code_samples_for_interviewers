using static contoso.functional.FnConstructs;
using Unit = System.ValueTuple;

namespace contoso.functional;

/// <summary>Placeholder struct.</summary>
public readonly struct NoValue { }

/// <summary>
/// An implementation of the Option type, which is a container that may or may not hold a value.
/// As a struct it can never be null and it's default value is <see cref="None"/>.
/// A non-empty instance can only be created via the <see cref="Some{T}"/> method.
/// Once constructed, interaction is possible only through the <see cref="Match{TResult}(Func{TResult}, Func{T, TResult})"/> method
/// or possibly the the <see cref="AsEnumerable"/> method.
/// </summary>
/// <remarks>This implementation follows the pattern optimized for CSharp suggested by Enrico Buonanno.</remarks>
public readonly struct Option<T> : IEquatable<NoValue>, IEquatable<Option<T>>
{
    readonly T? _value;
    readonly bool _isSome;

    /// <summary>Can only be instantiated by the static <see cref="Some{T}"/> factory method.</summary>
    internal Option(T value) => (_value, _isSome) = (value, value is not null);

    readonly bool IsNone => !_isSome;

    /// <summary>Yields a sequence of one, or an empty sequence when value is <see cref="None"/>.</summary>
    public IEnumerable<T> AsEnumerable()
    {
        if (_isSome)
            yield return _value!;
    }

    /// <summary>Extract the inner Some or None as <typeparamref name="TResult"/>.</summary>
    public TResult Match<TResult>(Func<TResult> None, Func<T, TResult> Some) => _isSome ? Some(_value!) : None();

    /// <summary>Execute an action with side-effects on Some or None.</summary>
    public Unit Match(Action None, Action<T> Some) => Match(None.ToFunc(), Some.ToFunc());

    /// <summary>Implicit conversion of <paramref name="value"/> into an <see cref="Option{T}"/>.</summary>
    public static implicit operator Option<T>(T value) => value is null ? None : new Option<T>(value);

    /// <summary>Implicit conversion from <see cref="NoValue"/> to an <see cref="Option{T}"/>.</summary>
    public static implicit operator Option<T>(NoValue _) => default;

    /// <inheritdoc/>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Option<T> left, Option<T> right) => !(left == right);

    /// <inheritdoc/>
    public static bool operator true(Option<T> src) => src._isSome;

    /// <inheritdoc/>
    public static bool operator false(Option<T> src) => src.IsNone;

    /// <inheritdoc/>
    public static Option<T> operator |(Option<T> left, Option<T> right) => left._isSome ? left : right;

    /// <inheritdoc/>
    public readonly bool Equals(NoValue _) => IsNone;

    /// <inheritdoc/>
    public readonly bool Equals(Option<T> other) => _isSome == other._isSome && (IsNone || EqualityComparer<T>.Default.Equals(_value, other._value));

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => IsNone ? 0 : EqualityComparer<T>.Default.GetHashCode(_value!);

    /// <inheritdoc/>
    public override readonly string ToString() => _isSome ? $"Some({_value!.ToString().Elided(64)})" : "None";
}
