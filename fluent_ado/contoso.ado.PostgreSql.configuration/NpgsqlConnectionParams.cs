using contoso.ado.Internals;

namespace contoso.ado.PostgreSql;


// Pooling: https://stackoverflow.com/a/44272654/458354
//          https://stackoverflow.com/q/65692743/458354

/// <summary>Wrapper to add Npgsql connection parameters. Leave null to use all defaults.</summary>
public class NpgsqlConnectionParams
{
    /// <summary>The time to wait (in seconds) while trying to establish a connection before terminating the attempt and generating an error. Default = 15.</summary>
    public int Timeout { get; set; } = 15;

    /// <summary>The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error. Set to zero for infinity. Default = 30.</summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>Determines the size of the internal buffer Npgsql uses when reading. Increasing may improve performance if transferring large values from the database. Default = 8192.</summary>
    public int ReadBufferSize { get; set; } = 8192;

    /// <summary>Determines the size of the internal buffer Npgsql uses when writing. Increasing may improve performance if transferring large values to the database. Default = 8192.</summary>
    public int WriteBufferSize { get; set; } = 8192;

    /// <summary>The optional application name parameter to be sent to the backend during connection initiation.</summary>
    public string ApplicationName { get; set; } = "contoso.ado";

    /// <summary>Whether connection pooling should be used. Default = true.</summary>
    public bool Pooling { get; set; } = true;

    /// <summary>The minimum connection pool size. Default = 0.</summary>
    public int MinimumPoolSize { get; set; } = 0;

    /// <summary>The maximum connection pool size. Default = 100.</summary>
    public int MaximumPoolSize { get; set; } = 100;

    /// <summary>The time (in seconds) to wait before closing idle connections in the pool if the count of all connections exceeds Minimum Pool Size. Default = 300.</summary>
    public int ConnectionIdleLifetime { get; set; } = 300;

    /// <summary>Custom flag to automatically inject a 'Prepare()' invocation with every command context. Default is true. Set false to allow explicitly invoking Prepare() on the PostgresCommandContext as needed only.</summary>
    public bool WithInjectedPrepare { get; set; } = true;

    /// <summary>When enabled, PostgreSQL error and notice details are included on PostgresException.Detail and PostgresNotice.Detail. These can contain sensitive data.</summary>
    public bool IncludeErrorDetail { get; set; } = false;

    /// <summary>
    /// Create a <see cref="NpgsqlConnectionParams"/> instance from the application's <see cref="PostgresConnectionInfo"/> configuration instance,
    /// supplying sensible defaults where properties are not supplied.
    /// </summary>
    public static NpgsqlConnectionParams From(PostgresConnectionInfo config)
    {
        // instance for supplying default values
        var dflt = new NpgsqlConnectionParams();
        // use the default instance if the configuration section is null or default
        if (config is null)
            return dflt;

        // create a new instance from the configuarion, supplying default values for empty properties
        return new NpgsqlConnectionParams
        {
            Timeout = DefaultIfZeroOrLess(config.ConnectionTimeoutSeconds, dflt.Timeout),
            CommandTimeout = DefaultIfZeroOrLess(config.CommandTimeoutSeconds, dflt.CommandTimeout),
            ReadBufferSize = DefaultIfZeroOrLess(config.ReadBufferSize, dflt.ReadBufferSize),
            WriteBufferSize = DefaultIfZeroOrLess(config.WriteBufferSize, dflt.WriteBufferSize),
            ApplicationName = ConnPoolName(config.ConnectionPoolTag, dflt.ApplicationName),
            Pooling = PoolingEnabled(config.EnablePooling, dflt.Pooling),
            // these only matter if pooling enabled, but we'll set them anyways
            MinimumPoolSize = DefaultIfZeroOrLess(config.MinimumPoolSize, dflt.MinimumPoolSize),
            MaximumPoolSize = DefaultIfZeroOrLess(config.MaximumPoolSize, dflt.MaximumPoolSize),
            ConnectionIdleLifetime = DefaultIfZeroOrLess(config.ConnectionIdleSeconds, dflt.ConnectionIdleLifetime),
            IncludeErrorDetail = ErrorDetailEnabled(config.IncludeErrorDetail, dflt.IncludeErrorDetail)
        };

        static int DefaultIfZeroOrLess(int? v, int defaultVal) => v.GetValueOrDefault() <= 0 ? defaultVal : v!.Value;

        static string ConnPoolName(string? suffix, string prefix) => suffix.IsNotNullOrEmptyString() ? $"{prefix}.{suffix}" : prefix;

        // pooling should be enabled unless explicitly disabled
        static bool PoolingEnabled(bool? cfg, bool dflt) => cfg == null ? dflt : cfg.Value;

        static bool ErrorDetailEnabled(bool? cfg, bool dflt) => cfg ?? dflt;
    }
}
