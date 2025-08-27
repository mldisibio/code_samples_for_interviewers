using System.Runtime.CompilerServices;
using Unit = System.ValueTuple;

namespace contoso.functional;

/// <summary>
/// A specialized <see cref="Either{L, R}"/> where 'Left' is an Exception and 'Right' is a value representing the expected success.
/// </summary>
public readonly struct Result<T>
{
    internal Result(Exception ex, Option<string> context = default, [CallerMemberName] string? calledFrom = "")
        => (Succeeded, CapturedException, Value, ExceptionContext, CalledFrom) =
           (
                false,
                ex ?? InvalidResultException.ForUninitializedException,
                default,
                context,
                calledFrom
            );

    internal Result(in T success, [CallerMemberName] string? calledFrom = "")
        => (Succeeded, CapturedException, Value, ExceptionContext, CalledFrom) =
           (
               success is not null,
               success is null ? InvalidResultException.ForNullSuccessValue : default,
               success,
               FnConstructs.None,
               calledFrom
           );

    readonly T? Value { get; }
    readonly Exception? CapturedException { get; }

    bool Succeeded { get; }
    bool Failed => !Succeeded;

    /// <summary>Method or property name of the caller creating this instance.</summary>
    public string? CalledFrom { get; init; }

    /// <summary>Optional diagnostic context for any exception encountered.</summary>
    public Option<string> ExceptionContext { get; init; }

    /// <summary>Implicit conversion from <see cref="Exception"/> to an exception <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(Exception ex) => new(ex);

    /// <summary>Implicit conversion from any <typeparamref name="T"/> to a success <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(T success) => new(success);

    /// <summary>Implicit conversion from <see cref="Failure"/> to an exception <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(Failure exWrapper) => new(exWrapper.Exception, exWrapper.Context, exWrapper.CalledFrom.EmptyIfNone());

    /// <inheritdoc/>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Result<T> left, Result<T> right) => !(left == right);

    Failure AsFailure() => new(CapturedException ?? InvalidResultException.ForUninitializedException, ExceptionContext, CalledFrom.Maybe());

    /// <summary>
    /// Execute <paramref name="Success"/> on <typeparamref name="T"/> if no exception was thrown, otherwise execute <paramref name="Failure"/>.
    /// </summary>
    public TResult Match<TResult>(Func<Failure, TResult> Failure, Func<T, TResult> Success)
        => Failed
           ? Failure(AsFailure())
           : Success(Value!);

    /// <summary>
    /// Execute <paramref name="Success"/> on <typeparamref name="T"/> if no exception was thrown, 
    /// otherwise execute <paramref name="Failure"/>, where a side-effect is expected of either./>.
    /// </summary>
    public Unit Match(Action<Failure> Failure, Action<T>? Success = null) => Match(Failure.ToFunc(), (Success ?? FnConstructs.DoNothing<T>()).ToFunc());

    /// <summary>Yields a sequence of one <see cref="Value"/> if valid, otherwise an empty sequence.</summary>
    public IEnumerator<T> AsEnumerableOfValid()
    {
        if (Succeeded)
            yield return Value!;
    }

    /// <inheritdoc/>
    public override string ToString()
        => Failed
           ? AsFailure().ToString()
           : $"{Value}".Elided(64);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Result<T> other
        && Succeeded == other.Succeeded
        && (Succeeded && EqualityComparer<T>.Default.Equals(Value, other.Value) || string.Equals(ToString(), other.ToString()));

    /// <inheritdoc/>
    public override int GetHashCode()
        => Match
        (
           Failure: ex => ex.GetHashCode(),
           Success: t => t!.GetHashCode()
        );
}