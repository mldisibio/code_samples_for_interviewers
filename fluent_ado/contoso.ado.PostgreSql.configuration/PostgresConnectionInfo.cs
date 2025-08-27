using contoso.ado.Internals;

namespace contoso.ado.PostgreSql;

/// <summary>Settings to connect to on-prem postgres, with latest Npgsql supported connection pool parameters.</summary>
public class PostgresConnectionInfo
{
    string? _cfgConnPoolTag;
    int? _cfgConnTimeoutSeconds;
    int? _cfgCmdTimeoutSeconds;
    int? _cfgReadBufferSize;
    int? _cfgWriteBufferSize;
    bool? _cfgEnablePooling;
    int? _cfgMinPoolSize;
    int? _cfgMaxPoolSize;
    int? _cfgConnIdleSeconds;
    bool? _cfgIncludeErrorDetail;

    readonly string? _envConnPoolTag;
    readonly int? _envConnTimeoutSeconds;
    readonly int? _envCmdTimeoutSeconds;
    readonly int? _envReadBufferSize;
    readonly int? _envWriteBufferSize;
    readonly bool? _envEnablePooling;
    readonly int? _envMinPoolSize;
    readonly int? _envMaxPoolSize;
    readonly int? _envConnIdleSeconds;
    readonly bool? _envIncludeErrorDetail;

    /// <summary>
    /// The properties will be initialized by the appsettings reader using the setters.
    /// When initialized, we will parse any environment variables at the same time.
    /// When returning a property value, any non-empty environment variable will override the appsettings value.
    /// </summary>
    public PostgresConnectionInfo()
    {
        _envConnPoolTag = EnvironmentKeys.ReadEnvAsString("PG_CONN_POOL_SUFFIX");
        _envConnTimeoutSeconds = EnvironmentKeys.ReadEnvAsInt32("PG_CONN_TIMEOUT");
        _envCmdTimeoutSeconds = EnvironmentKeys.ReadEnvAsInt32("PG_CMD_TIMEOUT");
        _envReadBufferSize = EnvironmentKeys.ReadEnvAsInt32("PG_READ_BUFFER");
        _envWriteBufferSize = EnvironmentKeys.ReadEnvAsInt32("PG_WRITE_BUFFER");
        _envEnablePooling = EnvironmentKeys.ReadEnvAsBool("PG_ENABLE_CONN_POOL");
        _envMinPoolSize = EnvironmentKeys.ReadEnvAsInt32("PG_MIN_POOL_SIZE");
        _envMaxPoolSize = EnvironmentKeys.ReadEnvAsInt32("PG_MAX_POOL_SIZE");
        _envConnIdleSeconds = EnvironmentKeys.ReadEnvAsInt32("PG_POOL_IDLE_TIMEOUT");
        _envIncludeErrorDetail = DeploymentEnvironmentVarIsDevOrStaging();
    }

    /// <summary>Host</summary>
    public string Host { get; set; } = default!;

    /// <summary>Port</summary>
    public ushort Port { get; set; } = 5432;

    /// <summary>Database</summary>
    public string Database { get; set; } = default!;

    /// <summary>User</summary>
    public string User { get; set; } = default!;

    /// <summary>Key</summary>
    public string Key { get; set; } = default!;

    /// <summary>The optional suffix appended to the application name parameter sent to the backend during connection initiation.</summary>
    public string? ConnectionPoolTag
    {
        get => _envConnPoolTag ?? _cfgConnPoolTag;
        set => _cfgConnPoolTag = value;
    }

    /// <summary>The time to wait (in seconds) while trying to establish a connection before terminating the attempt and generating an error. Default = 15.</summary>
    public int? ConnectionTimeoutSeconds
    {
        get => _envConnTimeoutSeconds ?? _cfgConnTimeoutSeconds;
        set => _cfgConnTimeoutSeconds = value;
    }

    /// <summary>The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error. Set to zero for infinity. Default = 30.</summary>
    public int? CommandTimeoutSeconds
    {
        get => _envCmdTimeoutSeconds ?? _cfgCmdTimeoutSeconds;
        set => _cfgCmdTimeoutSeconds = value;
    }

    /// <summary>Determines the size of the internal buffer Npgsql uses when reading. Increasing may improve performance if transferring large values from the database. Default = 8192.</summary>
    public int? ReadBufferSize
    {
        get => _envReadBufferSize ?? _cfgReadBufferSize;
        set => _cfgReadBufferSize = value;
    }

    /// <summary>Determines the size of the internal buffer Npgsql uses when writing. Increasing may improve performance if transferring large values to the database. Default = 8192.</summary>
    public int? WriteBufferSize
    {
        get => _envWriteBufferSize ?? _cfgWriteBufferSize;
        set => _cfgWriteBufferSize = value;
    }

    /// <summary>Whether connection pooling should be used. Default = true.</summary>
    public bool? EnablePooling
    {
        get => _envEnablePooling ?? _cfgEnablePooling;
        set => _cfgEnablePooling = value;
    }

    /// <summary>The minimum connection pool size. Default = 0.</summary>
    public int? MinimumPoolSize
    {
        get => _envMinPoolSize ?? _cfgMinPoolSize;
        set => _cfgMinPoolSize = value;
    }

    /// <summary>The maximum connection pool size. Default = 100.</summary>
    public int? MaximumPoolSize
    {
        get => _envMaxPoolSize ?? _cfgMaxPoolSize;
        set => _cfgMaxPoolSize = value;
    }

    /// <summary>The time (in seconds) to wait before closing idle connections in the pool if the count of all connections exceeds Minimum Pool Size. Default = 300.</summary>
    public int? ConnectionIdleSeconds
    {
        get => _envConnIdleSeconds ?? _cfgConnIdleSeconds;
        set => _cfgConnIdleSeconds = value;
    }

    /// <summary>Whether connection pooling should be used. Default = true.</summary>
    public bool? IncludeErrorDetail
    {
        get => _envIncludeErrorDetail ?? _cfgIncludeErrorDetail;
        set => _cfgIncludeErrorDetail = value;
    }

    static bool DeploymentEnvironmentVarIsDevOrStaging()
    {
        bool aspnetMatches = EnvironmentKeys.ReadEnvAsString("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant() is string aspnet && (string.Equals(aspnet, "development") || string.Equals(aspnet, "staging"));
        bool dotnetMatches = EnvironmentKeys.ReadEnvAsString("DOTNET_ENVIRONMENT")?.ToLowerInvariant() is string dotnet && (string.Equals(dotnet, "development") || string.Equals(dotnet, "staging"));
        return aspnetMatches || dotnetMatches;
    }
}
