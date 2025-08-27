namespace contoso.functional;

#pragma warning disable CS1591
public static partial class Either
{
    public readonly struct Left<L>
    {
        internal L Value { get; }
        internal Left(L value) { Value = value; }
        public override string ToString() => $"Left({Value})";
    }
}
#pragma warning restore CS1591