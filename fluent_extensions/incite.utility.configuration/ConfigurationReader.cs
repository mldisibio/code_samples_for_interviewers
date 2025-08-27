using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace contoso.utility.configuration;

/// <summary>
/// Allows any application to read the appsettings.json as <see cref="IConfiguration"/>
/// when there is no access to the configuration builder during services container startup.
/// </summary>
public static class ConfigurationReader
{
    const string _dotNetEnvironmentKey = "DOTNET_ENVIRONMENT";
    const string _aspNetCoreEnvironmentKey = "ASPNETCORE_ENVIRONMENT";

    static string? _executionDirectory;
    static string? _deploymentEnvironment;
    static IConfiguration? _appSettingsConfig;

    /// <summary>The configurable deployment environment string, such as 'Development', 'Staging', or 'Production'.</summary>
    public static string DeploymentEnvironment => GetDeploymentEnvironment();

    /// <summary>
    /// Read the <paramref name="sectionName"/> from the appsettings.json file
    /// and convert it to a new instance <typeparamref name="T"/>, or default(T) if <paramref name="sectionName"/> is not found.
    /// The appsettings.json file must be set to 'Copy to Output Directory'
    /// </summary>
    public static T? ReadSectionAs<T>(string sectionName) where T : new()
    {
        // this should simply throw any exception on failure as configuration is critical
        IConfiguration config = AppSettingsConfiguration();

        // GetSection never returns null. Returns empty ConfigurationSection if sectionName not found;
        // Get returns defaut(T) if an instance of T cannot be read from the configuration;
        return config.GetSection(sectionName).Get<T>();
    }

    /// <summary>
    /// Read the <paramref name="sectionName"/> from the appsettings.json file
    /// and convert it to a new instance <typeparamref name="T"/>, or default(T) if <paramref name="sectionName"/> is not found.
    /// The appsettings.json file must be set to 'Copy to Output Directory'
    /// </summary>
    public static T ReadSectionAs<T>(string sectionName, T defaultValue) where T : new() => ReadSectionAs<T>(sectionName) ?? defaultValue;

    /// <summary>
    /// Read the <paramref name="sectionName"/> from the appsettings.json file
    /// The appsettings.json file must be set to 'Copy to Output Directory'
    /// </summary>
    public static IConfigurationSection ReadSection(string sectionName) => AppSettingsConfiguration().GetSection(sectionName);

    /// <summary>Read a top-level value <paramref name="key"/> (not included in any section) from the appsettings.json file. Returns default(T) if key not found.</summary>
    public static T? ReadTopLevelValue<T>(string key) => AppSettingsConfiguration().GetValue<T>(key);

    /// <summary>Read a top-level value <paramref name="key"/> (not included in any section) from the appsettings.json file. Returns <paramref name="defaultValue"/> if key not found.</summary>
    public static T? ReadTopLevelValue<T>(string key, T defaultValue) => AppSettingsConfiguration().GetValue<T>(key, defaultValue!);

    /// <summary>Read a top-level value <paramref name="key"/> (not included in any section) as an array of type <typeparamref name="T"/>. Returns an empty array if key not found.</summary>
    public static T[] ReadTopLevelValueAsArray<T>(string key) => AppSettingsConfiguration().GetSection(key).Get<T[]>() ?? Array.Empty<T>();

    /// <summary>Create an out-of-band configuration reader that also knows to add the 'deployment' specific appsettings.{env}.json file.</summary>
    static IConfiguration AppSettingsConfiguration()
    {
        // this should simply throw any exception on failure as configuration is critical
        if (_appSettingsConfig == null)
        {
            string envString = GetDeploymentEnvironment();
            string? executionDir = GetExecutionDirectory();

            // fluent syntax is:
            // IConfiguration temp = configBuilder.SetBasePath(<path>).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();

            // see if we can set the base path, otherwise, allow it to be inferred 
            // note: If SetBasePath(..) is not called, it is inferred from application base. https://github.com/aspnet/Announcements/issues/88

            IConfigurationBuilder configBuilder = executionDir == null
                                                  ? new ConfigurationBuilder()
                                                  : new ConfigurationBuilder().SetBasePath(executionDir);

            _appSettingsConfig = configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                                              .AddJsonFile($"appsettings.{envString}.json", optional: true, reloadOnChange: true)
                                              .Build();
            if (_appSettingsConfig == null)
                throw new InvalidOperationException("No IConfiguration could be read from appsettings.json");
        }
        return _appSettingsConfig;
    }

    /// <summary>Environment variables (usually supplied at runtime) override any static, file based configuration.</summary>
    /// <remarks>
    /// See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-3.1
    /// Should return 'Development', 'Staging', 'Production', although custom names are also supported.
    /// </remarks>
    static string GetDeploymentEnvironment()
    {
        if (string.IsNullOrEmpty(_deploymentEnvironment))
        {
            string? bestDeploymentValue = null;
            try
            {

                bestDeploymentValue = Environment.GetEnvironmentVariable(_aspNetCoreEnvironmentKey);
                if (string.IsNullOrWhiteSpace(bestDeploymentValue))
                    bestDeploymentValue = Environment.GetEnvironmentVariable(_dotNetEnvironmentKey);
                // if not set as an environment variable, read it from the appsettions.json (which is our convention)
                if (string.IsNullOrWhiteSpace(bestDeploymentValue))
                    bestDeploymentValue = GetDeploymentEnvironmentFromAppSettings();
            }
            catch { }
            // default to 'Production' as does Microsoft
            _deploymentEnvironment = String.IsNullOrWhiteSpace(bestDeploymentValue) ? "Production" : bestDeploymentValue.Trim();
        }
        return _deploymentEnvironment;
    }

    /// <summary>
    /// If 'DOTNET_ENVIRONMENT' or 'ASPNETCORE_ENVIRONMENT' is not set as an environment variable at runtime,
    /// attempt to read it from the primary/production appsetting.json file itself.
    /// A key with the name '"DOTNET_ENVIRONMENT' or 'ASPNETCORE_ENVIRONMENT' is expected the top level of the root 'appsettings.json' file
    /// (and is ignored if overwritten in any child appsettings files).
    /// </summary>
    static string? GetDeploymentEnvironmentFromAppSettings()
    {
        string? deploymentValue = null;
        try
        {
            // used the ConfigurationBuilder to read appsettings.json once just for this key/value
            // note, we don't keep this configuration because it is from 'production' and we might not be in a production deployment;
            // we are using it *only* to read the environment variable, if needed
            string? executionDir = GetExecutionDirectory();

            // fluent syntax is:
            // IConfiguration temp = configBuilder.SetBasePath(<path>).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();

            // see if we can set the base path, otherwise, allow it to be inferred 
            IConfigurationBuilder configBuilder = executionDir == null
                                                  ? new ConfigurationBuilder()
                                                  : new ConfigurationBuilder().SetBasePath(executionDir);

            IConfiguration temp = configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();

            deploymentValue = temp.GetValue<string?>(_aspNetCoreEnvironmentKey, null);
            if (string.IsNullOrWhiteSpace(deploymentValue))
                deploymentValue = temp.GetValue<string?>(_dotNetEnvironmentKey, null);
        }
        catch { }
        return string.IsNullOrWhiteSpace(deploymentValue) ? null : deploymentValue.Trim();
    }

    /// <summary>The directory containing code that is currently running</summary>
    static string? GetExecutionDirectory()
    {
        if (_executionDirectory == null)
        {
            try
            {
                string? candidatePath = AppContext.BaseDirectory;
                if (PathHasAppsettings(candidatePath, out string? canonical))
                {
                    _executionDirectory = canonical;
                    return _executionDirectory;
                }
            }
            catch { }

            //topMostAssembly = Assembly.GetEntryAssembly() ?? (Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly());
            try
            {
                string? assemblyLocation = Assembly.GetEntryAssembly()?.Location;
                if (assemblyLocation != null && PathHasAppsettings(Path.GetDirectoryName(assemblyLocation), out string? canonical))
                {
                    _executionDirectory = canonical;
                    return _executionDirectory;
                }
            }
            catch { }

            try
            {
                string? assemblyLocation = Assembly.GetCallingAssembly()?.Location;
                if (assemblyLocation != null && PathHasAppsettings(Path.GetDirectoryName(assemblyLocation), out string? canonical))
                {
                    _executionDirectory = canonical;
                    return _executionDirectory;
                }
            }
            catch { }

            try
            {
                string? assemblyLocation = Assembly.GetExecutingAssembly()?.Location;
                if (assemblyLocation != null && PathHasAppsettings(Path.GetDirectoryName(assemblyLocation), out string? canonical))
                {
                    _executionDirectory = canonical;
                    return _executionDirectory;
                }
            }
            catch { }
        }
        return _executionDirectory;
    }

    static bool PathHasAppsettings(string? path, out string? canonical)
    {
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var dirInfo = new DirectoryInfo(Path.GetFullPath(path!));
                if (dirInfo.Exists)
                    if (dirInfo.EnumerateFiles(searchPattern: "appsettings*.json", searchOption: SearchOption.TopDirectoryOnly).Any())
                    {
                        canonical = Path.TrimEndingDirectorySeparator(dirInfo.FullName);
                        return true;
                    }
            }
            catch { }
        }
        canonical = null;
        return false;
    }
}
