namespace contoso.sqlite.raw
{
    /// <summary>Bind values to parameters of a prepared statement.</summary>
    public sealed class BindContext
    {
        readonly StatementContext _ctx;
        internal BindContext(StatementContext ctx) => _ctx = ctx;

        /// <summary>Create a context for binding values to parameter named <paramref name="paramName"/>.</summary>
        public ParameterContext Bind(string paramName) => Bind(_ctx[paramName]);

        /// <summary>Create a context for binding values to parameter at index <paramref name="paramIdx"/>.</summary>
        public ParameterContext Bind(int paramIdx) => new ParameterContext(paramIdx, _ctx);
    }
}
