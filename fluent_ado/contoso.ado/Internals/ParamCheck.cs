namespace contoso.ado.Internals;

/// <summary>Fluent argument validation helper.</summary>
internal static class ParamCheck
{
    const string _nullArgumentMsg = "The required {0} argument '{1}' is null.";
    const string _emptyStringMsg = "The required string argument '{0}' is empty.";
    const string _defaultArgDesc = "[supplied argument]";
    const string _defaultArgName = "?";

    /// <summary>Check a parameter and throw an exception if necessary.</summary>
    public static class Assert
    {
        /// <summary>Throws an <see cref="ArgumentException"/> if the given string parameter is null, empty or whitespace.</summary>
        public static void IsNotNullOrEmpty(string arg, string argName)
        {
            if (arg == null)
                throw new ArgumentNullException(argName ?? _defaultArgDesc, string.Format(_nullArgumentMsg, "string", argName ?? _defaultArgName));

            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentException(string.Format(_emptyStringMsg, argName ?? _defaultArgName));
        }

        /// <summary>Throws an <see cref="ArgumentNullException"/> if the given parameter is null.</summary>
        /// <remarks>Does not check if a string is empty or whitespace only.</remarks>
        public static void IsNotNull<T>(T arg, string argName)
            where T : class
        {
            if (arg == null)
            {
                throw new ArgumentNullException(argName ?? _defaultArgDesc, string.Format(_nullArgumentMsg, typeof(T).Name, argName ?? _defaultArgName));
            }
        }
    }

    /// <summary>Inline, chained argument check.</summary>
    public static string ThrowIfNullOrEmpty(this string arg, string argName)
    {
        Assert.IsNotNullOrEmpty(arg, argName);
        return arg;
    }
}