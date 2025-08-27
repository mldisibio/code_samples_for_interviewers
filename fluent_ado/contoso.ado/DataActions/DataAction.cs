namespace contoso.ado.DataActions
{
    /// <summary>Constant expressions for actions that do nothing with a mapped DTO.</summary>
    public static class DataAction
    {
        /// <summary>Constant expression for an action that does nothing with a mapped DTO.</summary>
        public static void DoNothing<T>(T _) { }
    }
}
