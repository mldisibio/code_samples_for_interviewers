using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Unit = System.ValueTuple;

namespace contoso.functional;
/// <summary></summary>
public static partial class FnConstructs
{
    /// <summary>Use in place of 'void' for functional patterns.</summary>
    public static Unit Unit() => default;

    /// <summary>Any <see cref="Option{T}"/> with no value.</summary>
    public static NoValue None => default;

    /// <summary>Discard <typeparamref name="T"/> and do nothing.</summary>
    public static Action<T> DoNothing<T>() => _ => { };

    /// <summary>Lift <typeparamref name="T"/> into an <see cref="Option{T}"/>. <paramref name="value"/> is guaranteed to be non null if no exception is thrown.</summary>
    public static Option<T> Some<T>([NotNull] T? value) => new(value ?? throw new ArgumentNullException(nameof(value)));

    /// <summary>Lift <typeparamref name="L"/> into an <see cref="Either{L, R}"/> that has 'Left' the happy path for more sinister things.</summary>
    public static Either.Left<L> Left<L>(L l) => new Either.Left<L>(l);

    /// <summary>Lift <typeparamref name="R"/> into an <see cref="Either{L, R}"/> that has followed the happy 'Right' path.</summary>
    public static Either.Right<R> Right<R>(R r) => new Either.Right<R>(r);

    /// <summary>Lift <typeparamref name="T"/> into a monadic container of <see cref="IEnumerable{T}"/>.</summary>
    public static Func<T, IEnumerable<T>> ListOfOne<T>() => t => ListOf(t);

    /// <summary>Shortcut for initializing a list.</summary>
    /// <example>
    /// <code>
    /// var empty = FnConstructs.List&lt;int&gt;();
    /// var single = FnConstructs.List(1);
    /// var many = FnConstructs.List(1, 2, 3);
    /// </code>
    /// </example>
    public static IEnumerable<T> ListOf<T>(params T[] items) => items.ToImmutableList();

    /// <summary>Wrap <paramref name="message"/> as <see cref="functional.Error"/>.</summary>
    public static Error Error(string message) => functional.Error.Of(message);

    /// <summary>Lift <typeparamref name="T"/> into a <see cref="Validation{T}"/> in the valid state with a non null <typeparamref name="T"/>.</summary>
    public static Validation<T> Valid<T>([NotNull] T value) => new(value ?? throw new ArgumentNullException(nameof(value)));

    /// <summary>Lift <typeparamref name="T"/> a <see cref="Result{T}"/> in the success state with a non null <typeparamref name="T"/>.</summary>
    public static Result<T> Result<T>([NotNull] T value) => new(value ?? throw new ArgumentNullException(nameof(value)));

    /// <summary>Lift <paramref name="f"/> into a <c>Try</c> to be <c>Run</c> with exception handling.</summary>
    public static Try<T> Try<T>(Func<T> f) => () => f();

    /// <summary>Lift <paramref name="t"/> into a <see cref="Task{T}"/> for the sake of treating <see cref="Task{T}"/> as a monadic container.</summary>
    public static Task<T> Async<T>(T t) => Task.FromResult(t);

}