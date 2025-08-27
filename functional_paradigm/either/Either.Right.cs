namespace contoso.functional;

public static partial class Either
{
    /// <summary></summary>
    public readonly struct Right<R>
    {
        internal R Value { get; }
        internal Right(R value) { Value = value; }

        /// <inheritdoc/>
        public override string ToString() => $"Right({Value})";

        /// <summary>Apply <paramref name="map"/> to the inner <typeparamref name="R"/> of this instance and return the result as <see cref="Right{ROut}"/></summary>
        public Right<ROut> Map<LIn, ROut>(Func<R, ROut> map) => FnConstructs.Right(map(Value));

        /// <summary>Chain this instance to <paramref name="next"/> by passing its inner <typeparamref name="R"/> as input to <paramref name="next"/> and return an <see cref="Either{L, Rout}"/>.</summary>
        public Either<L, ROut> Bind<L, ROut>(Func<R, Either<L, ROut>> next) => next(Value);
    }
}
