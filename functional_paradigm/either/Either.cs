using Unit = System.ValueTuple;

namespace contoso.functional;

/// <summary>
/// Captures either of two possible outcomes, rather than representing failure as a 'None'.
/// By convention, 'Right' is the happy path outcome, and 'Left' is some sinister alternative.
/// </summary>
public readonly struct Either<L, R>
{
    L? Left { get; }
    R? Right { get; }

    bool IsRight { get; }
    readonly bool IsLeft => !IsRight;

    /// <summary>Intialize with a 'left' <typeparamref name="L"/> which cannot be null. The inner <typeparamref name="R"/> will be set to its default.</summary>
    internal Either(L left)
        => (IsRight, Left, Right) = (false, left ?? throw new ArgumentNullException(nameof(left)), default);

    /// <summary>Intialize with a 'right' <typeparamref name="R"/> which cannot be null. The inner <typeparamref name="L"/> will be set to its default.</summary>
    internal Either(R right)
        => (IsRight, Left, Right) = (true, default, right ?? throw new ArgumentNullException(nameof(right)));

    /// <inheritdoc/>
    public static implicit operator Either<L, R>(L left) => new Either<L, R>(left);
    /// <inheritdoc/>
    public static implicit operator Either<L, R>(R right) => new Either<L, R>(right);

    /// <inheritdoc/>
    public static implicit operator Either<L, R>(Either.Left<L> left) => new Either<L, R>(left.Value);
    /// <inheritdoc/>
    public static implicit operator Either<L, R>(Either.Right<R> right) => new Either<L, R>(right.Value);

    /// <summary>
    /// Apply <paramref name="Right"/> if the instance holds a non-null <typeparamref name="R"/>, otherwise apply <paramref name="Left"/>, to return a <typeparamref name="TResult"/>.
    /// </summary>
    public TResult Match<TResult>(Func<L, TResult> Left, Func<R, TResult> Right)
        => IsLeft
           ? Left(this.Left!)
           : Right(this.Right!);

    /// <summary>
    /// Execute <paramref name="Right"/> if the instance holds a non-null <typeparamref name="R"/>, otherwise execute <paramref name="Left"/>, where a side-effect is expected of either./>.
    /// </summary>
    public Unit Match(Action<L> Left, Action<R> Right)
        => Match
        (
            Left: Left.ToFunc(),
            Right: Right.ToFunc()
        );

    /// <summary>Yields a sequence of one <typeparamref name="R"/>, or an empty sequence if the instance is a <typeparamref name="L"/>.</summary>
    public IEnumerator<R> AsEnumerable()
    {
        if (IsRight)
            yield return Right!;
    }

    /// <inheritdoc/>
    public override string ToString() => Match(Left: l => $"Left({l})", Right: r => $"Right({r})");
}
