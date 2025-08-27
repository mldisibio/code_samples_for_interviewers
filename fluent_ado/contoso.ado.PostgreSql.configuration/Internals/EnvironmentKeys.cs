namespace contoso.ado.Internals;

/// <summary>Common helper methods for reading environment variables that may or may not exist.</summary>
internal static class EnvironmentKeys
{
    /// <summary>DOTNET_ENVIRONMENT</summary>
    public const string DotNetEnvironment = "DOTNET_ENVIRONMENT";

    /// <summary>ASPNETCORE_ENVIRONMENT</summary>
    public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

    /// <summary>Read environment variable <paramref name="envName"/> as a string. Returns null if environment variable is not defined.</summary>
    public static string? ReadEnvAsString(string envName)
    {
        string? envText = Environment.GetEnvironmentVariable(envName);
        return envText.IsNullOrEmptyString() ? null : envText.Trim();
    }

    /// <summary>Read environment variable <paramref name="envName"/> as a nullable Int32. Returns null if environment variable is not defined.</summary>
    public static int? ReadEnvAsInt32(string envName)
    {
        string? envText = Environment.GetEnvironmentVariable(envName);
        return (envText.IsNotNullOrEmptyString() && int.TryParse(envText, out int intVal)) ? intVal : (int?)null;
    }

    /// <summary>Read environment variable <paramref name="envName"/> as a nullable Int64. Returns null if environment variable is not defined.</summary>
    public static long? ReadEnvAsInt64(string envName)
    {
        string? envText = Environment.GetEnvironmentVariable(envName);
        return (envText.IsNotNullOrEmptyString() && long.TryParse(envText, out long longVal)) ? longVal : (long?)null;
    }

    /// <summary>Read environment variable <paramref name="envName"/> as a nullable boolean. Returns null if environment variable is not defined.</summary>
    public static bool? ReadEnvAsBool(string envName)
    {
        string? envText = Environment.GetEnvironmentVariable(envName).NullIfEmptyElseTrimmed();
        if (envText == null)
            return null;
        if (bool.TryParse(envText, out bool envBool))
            return envBool;
        if (int.TryParse(envText, out int envInt))
            return envInt > 0;
        return null;
    }
}
