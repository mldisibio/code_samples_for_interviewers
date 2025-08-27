using Unit = System.ValueTuple;

namespace contoso.functional;

/// <summary>
/// A specialized <see cref="Either{L, R}"/> where 'Left' is a collection of validation errors and 'Right' is a validated value where all validation has passed.
/// </summary>
public struct Validation<T>
{
    IEnumerable<Error>? _errors = null;

    Validation(IEnumerable<Error> errors)
    {
        IsValid = false;
        Value = default;
        Errors = errors;
    }

    internal Validation(T t)
    {
        IsValid = true;
        Value = t;
        Errors = Enumerable.Empty<Error>();
    }

    /// <summary>True if the instance wraps a non-null value in the valid state.</summary>
    public bool IsValid { get; }

    internal T? Value { get; }

    internal IEnumerable<Error> Errors
    {
        get => _errors ??= IsValid ? Enumerable.Empty<Error>() : new[] { Error.Default };
        init => _errors = value;
    }

    /// <summary>Create a <see cref="Validation{T}"/> in the invalid state.</summary>
    public static Validation<T> Fail(IEnumerable<Error> errors) => new(errors ?? [Error.Default]);

    /// <summary>Create a <see cref="Validation{T}"/> in the invalid state.</summary>
    public static Validation<T> Fail(params Error[] errors) => new(errors.AsEnumerable());

    /// <summary>Implicit conversion from <see cref="Error"/> to <see cref="Validation{T}"/> in the invalid state.</summary>
    public static implicit operator Validation<T>(Error error) => new(new[] { error });

    /// <summary>Implicit conversion from <see cref="Validation.Invalid"/> to <see cref="Validation{T}"/> in the invalid state.</summary>
    public static implicit operator Validation<T>(Validation.Invalid invalid) => new Validation<T>(invalid.Errors);

    /// <summary>Implicit conversion from any <typeparamref name="T"/> to <see cref="Validation{T}"/> in the valid state.</summary>
    public static implicit operator Validation<T>(T valid) => FnConstructs.Valid(valid);

    /// <inheritdoc/>
    public static bool operator ==(Validation<T> left, Validation<T> right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Validation<T> left, Validation<T> right) => !(left == right);

    /// <summary>
    /// Execute <paramref name="Valid"/> if the instance holds a non-null, valid <typeparamref name="T"/>, otherwise execute <paramref name="Invalid"/> on <see cref="Errors"/>.
    /// </summary>
    public TResult Match<TResult>(Func<IEnumerable<Error>, TResult> Invalid, Func<T, TResult> Valid)
        => IsValid ? Valid(Value!) : Invalid(Errors);

    /// <summary>
    /// Execute <paramref name="Valid"/> if the instance holds a non-null, valid <typeparamref name="T"/>, 
    /// otherwise execute <paramref name="Invalid"/> on <see cref="Errors"/>, where a side-effect is expected of both./>.
    /// </summary>
    public Unit Match(Action<IEnumerable<Error>> Invalid, Action<T> Valid)
        => Match(Invalid.ToFunc(), Valid.ToFunc());

    /// <summary>Yields a sequence of one <see cref="Value"/> if valid, otherwise an empty sequence.</summary>
    public readonly IEnumerator<T> AsEnumerableOfValid()
    {
        if (IsValid)
            yield return Value!;
    }

    /// <inheritdoc/>
    public override string ToString()
        => IsValid
           ? $"Valid[{Value}]"
           : $"Invalid[{string.Join(", ", Errors)}]";

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Validation<T> other
        && IsValid == other.IsValid
        && (IsValid && EqualityComparer<T>.Default.Equals(Value, other.Value) || string.Equals(ToString(), other.ToString()));

    /// <inheritdoc/>
    public override int GetHashCode()
        => Match
        (
           Invalid: errs => errs.GetHashCode(),
           Valid: t => t!.GetHashCode()
        );
}


public static partial class FnConstructs
{
    /// <summary>Create a Validation in the invalid state without requiring an explicit generic type..</summary>
    public static Validation.Invalid Invalid(IEnumerable<Error> errors) => new(errors);
    /// <summary>Create a Validation in the invalid state without requiring an explicit generic type..</summary>
    public static Validation.Invalid Invalid(params Error[] errors) => new(errors);
    /// <summary>Create a <see cref="Validation{T}"/> in the invalid state.</summary>
    public static Validation<T> Invalid<T>(IEnumerable<Error> errors) => new Validation.Invalid(errors);
    /// <summary>Create a <see cref="Validation{T}"/> in the invalid state.</summary>
    public static Validation<T> Invalid<T>(params Error[] errors) => new Validation.Invalid(errors);
}