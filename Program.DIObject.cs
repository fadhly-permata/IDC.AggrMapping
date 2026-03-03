using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.Data;
using IDC.Utilities.Plugins;
using MongoDB.Driver;

namespace IDC.AggrMapping;

internal partial class Program
{
    /// <summary>
    /// Configures and registers caching service
    /// </summary>
    /// <param name="builder">The web application builder instance</param>
    /// <remarks>
    /// Sets up in-memory caching with configuration from appconfigs.jsonc:
    ///
    /// Example configuration:
    /// <code>
    /// {
    ///   "DependencyInjection": {
    ///     "Caching": {
    ///       "Enable": true,
    ///       "ExpirationInMinutes": 30
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// > [!NOTE]
    /// > Caching service is optional and will only be registered if enabled in configuration
    /// </remarks>
    /// <seealso cref="Caching"/>
    /// <seealso href="https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory">Memory caching in ASP.NET Core</seealso>
    private static void ConfigureCaching(WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.Caching.Enable", defaultValue: false))
            builder.Services.AddSingleton(static _ => new Caching(
                defaultExpirationMinutes: _appConfigurations.Get(
                    path: "DependencyInjection.Caching.ExpirationInMinutes",
                    defaultValue: 30
                )
            ));
    }

    /// <summary>
    /// Configures and registers the plugin manager service for the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder"/> instance used to register services.
    /// </param>
    /// <param name="systemLogging">
    /// The <see cref="SystemLogging"/> instance for logging plugin operations.
    /// </param>
    /// <remarks>
    /// Registers <see cref="PluginManager"/> as a singleton service if plugins are enabled in
    /// <c>appconfigs.jsonc</c>. The plugin manager is initialized with the provided logging instance
    /// and registers plugins from the configured source directory. The source directory is determined
    /// by combining <c>Plugins.SourceDirectory</c> from configuration with a default fallback path.
    /// <br/>
    /// Example configuration:
    /// <code>
    /// {
    ///   "Plugins": {
    ///     "Enable": true,
    ///     "SourceDirectory": "wwwroot/plugins/source"
    ///   }
    /// }
    /// </code>
    /// <br/>
    /// > [!NOTE]
    /// > The plugin manager will not be registered if <c>Plugins.Enable</c> is set to <c>false</c>.
    /// <br/>
    /// > [!TIP]
    /// > Ensure that the plugin source directory exists and contains valid plugins.
    /// <br/>
    /// > [!IMPORTANT]
    /// > The <see cref="PluginManager"/> is registered as a singleton to maintain plugin state
    ///   throughout the application's lifetime.
    /// </remarks>
    /// <returns>
    /// Registers the <see cref="PluginManager"/> service for dependency injection.
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown if the plugin source directory does not exist.
    /// </exception>
    /// <seealso cref="PluginManager"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection">Dependency Injection in .NET</seealso>
    private static void ConfigurePlugins(WebApplicationBuilder builder, SystemLogging systemLogging)
    {
        if (_appConfigurations.Get(path: "Plugins.Enable", defaultValue: false))
        {
            builder.Services.AddSingleton(implementationFactory: _ =>
            {
                var plugin = new PluginManager(systemLogging: systemLogging);
                plugin.Initialize(
                    sourceCodeDir: Path.Combine(
                        path1: _appConfigurations
                            .Get(
                                path: "Plugins.BaseDirectory",
                                defaultValue: Directory.GetCurrentDirectory()
                            )!
                            .RefinePlatformPath(),
                        path2: _appConfigurations
                            .Get(
                                path: "Plugins.SourceDirectory",
                                defaultValue: "wwwroot/plugins/source"
                            )!
                            .RefinePlatformPath()
                    ),
                    pluginNames: _appConfigurations.Get<string[]>(
                        path: "Plugins.EnabledPlugins",
                        defaultValue: ["All"]
                    )
                );
                return plugin;
            });
        }
    }

    /// <summary>
    /// Configures and registers the PostgreSQL database service for the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder"/> instance used to register services.
    /// </param>
    /// <remarks>
    /// Registers <see cref="PostgreHelper"/> as a scoped service if PostgreSQL is enabled in
    /// <c>appconfigs.jsonc</c>. The connection string is retrieved from <c>appsettings.json</c>
    /// using the configured key in <c>DefaultConStrings.IDCAggrMapping.PGSQL</c>. Logging is attached
    /// if <c>Logging.RegisterAsDI</c> is enabled.
    /// <br/>
    /// Example configuration:
    /// <code>
    /// {
    ///   "DependencyInjection": {
    ///     "PGSQL": true
    ///   },
    ///   "DefaultConStrings": {
    ///     "IDCAggrMapping": {
    ///       "PGSQL": "ConnectionString_en"
    ///     }
    ///   },
    ///   "DbContextSettings": {
    ///     "ConnectionString_en": "Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword"
    ///   },
    ///   "Logging": {
    ///     "RegisterAsDI": true
    ///   }
    /// }
    /// </code>
    /// <br/>
    /// > [!NOTE]
    /// > The PostgreSQL service will not be registered if <c>DependencyInjection.PGSQL</c> is set to <c>false</c>.
    /// <br/>
    /// > [!TIP]
    /// > Ensure the connection string is valid and the database is accessible.
    /// <br/>
    /// > [!IMPORTANT]
    /// > <see cref="PostgreHelper"/> is registered as scoped to ensure per-request database context.
    /// </remarks>
    /// <returns>
    /// Registers the <see cref="PostgreHelper"/> service for dependency injection.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the connection string is missing or invalid.
    /// </exception>
    /// <seealso cref="PostgreHelper"/>
    /// <seealso href="https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/">DbContext Configuration in .NET</seealso>
    private static void ConfigurePGSQL(WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.PGSQL", defaultValue: false))
            builder.Services.AddScoped(implementationFactory: static _ =>
            {
                string defaultConString = _appSettings.Get(
                    path: "DefaultConStrings.IDCAggrMapping.PGSQL",
                    defaultValue: "ConnectionString_en"
                );

                return new PostgreHelper(
                    connectionString: new CommonConnectionString()
                        .FromConnectionString(
                            connectionString: _appSettings.Get(
                                path: $"DbContextSettings.{defaultConString}",
                                defaultValue: "User ID=idc_fadhly;Password={pass};HOST=localhost;Port=5432;Database=idc.en;Pooling=true;MinPoolSize=1;MaxPoolSize=1000;"
                            )
                        )
                        .ChangePassword(
                            newPassword: (
                                _appSettings.Get<string?>(path: "configPass.passwordDB")
                                ?? throw new InvalidOperationException(
                                    "Failed to retrieve database password."
                                )
                            ).LegacyDecryptor(
                                _appSettings.Get(
                                    path: "KeyConvert.DecryptionKey",
                                    defaultValue: "idxpartners"
                                )
                            )
                        )
                        .ToPostgreSQL(),
                    logging: _appConfigurations.Get(
                        path: "Logging.RegisterAsDI",
                        defaultValue: true
                    )
                        ? _systemLogging
                        : null
                );
            });
    }

    /// <summary>
    /// Configures and registers SQLite database service
    /// </summary>
    /// <param name="builder">The web application builder instance</param>
    /// <remarks>
    /// Initializes SQLite connection with configuration from appsettings.json:
    ///
    /// Example configuration:
    /// <code>
    /// {
    ///   "SqLiteContextSettings": {
    ///     "memory": "Data Source=:memory:;Cache=Private;Mode=Memory",
    ///     "file": "Data Source=database.db;Cache=Shared;Mode=ReadWrite"
    ///   },
    ///   "DefaultConStrings": {
    ///     "SQLite": "memory"
    ///   }
    /// }
    /// </code>
    ///
    /// > [!TIP]
    /// > Use memory mode for testing and temporary data storage
    ///
    /// > [!WARNING]
    /// > Ensure proper file permissions when using file-based SQLite
    /// </remarks>
    /// <seealso cref="SQLiteHelper"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/">SQLite Database Provider</seealso>
    private static void ConfigureSQLite(WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.SQLite", defaultValue: false))
            builder.Services.AddScoped(implementationFactory: static _ =>
            {
                string defaultConString = _appSettings.Get(
                    path: "DefaultConStrings.IDCAggrMapping.SQLite",
                    defaultValue: "memory"
                );

                var logging = _appConfigurations.Get(
                    path: "Logging.RegisterAsDI",
                    defaultValue: true
                )
                    ? _systemLogging
                    : null;

                return defaultConString == "memory"
                    ? new SQLiteHelper(logging: logging)
                    : new SQLiteHelper(
                        connectionString: new CommonConnectionString()
                            .FromConnectionString(
                                connectionString: _appSettings.Get(
                                    path: $"SqLiteContextSettings.{defaultConString}",
                                    defaultValue: "memory"
                                )
                            )
                            .ToSQLite(),
                        logging: logging
                    );
            });
    }

    /// <summary>
    /// Configures and registers MongoDB database service
    /// </summary>
    /// <param name="builder">The web application builder instance</param>
    /// <remarks>
    /// Sets up MongoDB connection with configuration from appsettings.json:
    ///
    /// Example configuration:
    /// <code>
    /// {
    ///   "MongoDBSettings": {
    ///     "local": "mongodb://localhost:27017",
    ///     "production": "mongodb://user:password@host:port/database"
    ///   },
    ///   "DefaultConStrings": {
    ///     "MongoDB": "local"
    ///   }
    /// }
    /// </code>
    ///
    /// > [!IMPORTANT]
    /// > Connection timeout is set to 5 seconds by default
    ///
    /// > [!CAUTION]
    /// > Ensure proper security measures when storing connection strings
    /// </remarks>
    /// <seealso cref="IMongoDatabase"/>
    /// <seealso href="https://www.mongodb.com/docs/drivers/csharp/current/">MongoDB .NET Driver</seealso>
    private static void ConfigureMongoDB(WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.MongoDB", defaultValue: false))
            builder.Services.AddScoped(static _ =>
            {
                string defaultConString = _appSettings.Get(
                    path: "DefaultConStrings.IDCAggrMapping.MongoDB",
                    defaultValue: "local"
                );

                var settings = MongoClientSettings.FromConnectionString(
                    _appSettings.Get(
                        path: $"MongoDBSettings.{defaultConString}",
                        defaultValue: "mongodb://localhost:27017"
                    )
                );
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(value: 0b101);
                settings.ConnectTimeout = TimeSpan.FromSeconds(value: 0b101);
                settings.SocketTimeout = TimeSpan.FromSeconds(value: 0b101);
                settings.RetryWrites = false;
                settings.DirectConnection = defaultConString != "withReplica";

                return new MongoClient(settings).GetDatabase(settings.ApplicationName ?? "IDC_EN");
            });
    }
}
