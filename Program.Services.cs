using Hangfire;
using Hangfire.PostgreSql;
using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Comm.Http;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.API;
using IDC.Utilities.Models.Data;
using IDC.Utilities.Plugins;
using IDC.Utilities.Template.ConfigAndSettings;
using IDC.Utilities.Template.Middlewares.Securities.ApiKey;
using IDC.Utilities.Template.Middlewares.Validations;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace IDC.AggrMapping;

internal partial class Program
{
    private static WebApplicationBuilder SetupServices(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder
            .Services.AddControllers(configure: options =>
            {
                const string ContentType = "application/json";

                // Tambahkan filter untuk model state validation
                options.Filters.Add(item: new GenericModelValidation());

                options.Filters.Add(item: new ConsumesAttribute(contentType: ContentType));
                options.Filters.Add(item: new ProducesAttribute(contentType: ContentType));
                options.Filters.Add(item: new ProducesResponseTypeAttribute(statusCode: 200));
                options.Filters.Add(
                    item: new ProducesResponseTypeAttribute(
                        type: typeof(APIResponseData<List<string>?>),
                        statusCode: StatusCodes.Status400BadRequest
                    )
                );
                options.Filters.Add(
                    item: new ProducesResponseTypeAttribute(
                        type: typeof(APIResponseData<List<string>?>),
                        statusCode: StatusCodes.Status500InternalServerError
                    )
                );
            })
            .AddNewtonsoftJson(setupAction: options =>
            {
                options.SerializerSettings.ContractResolver =
                    new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        // Add CORS policy
        if (_appConfigurations.Get<bool>(path: "Middlewares.Cors.Enabled"))
        {
            builder.Services.AddCors(setupAction: options =>
            {
                options.AddPolicy(
                    name: $"{AppNameTrimmed()}-CorsPolicy",
                    configurePolicy: builder => { }
                );
            });
        }

        return builder;
    }

    private static WebApplicationBuilder SetupAppConfigurations(this WebApplicationBuilder builder)
    {
        _appConfigurations = new AppConfigurations(
            appName: CON_STR_APP_NAME,
            jsoncFilePath: Path.Combine(
                    path1: Directory.GetCurrentDirectory().RefinePlatformPath(),
                    path2: "appconfigs.jsonc"
                )
                .RefinePlatformPath(),
            sqliteDbPath: Path.Combine(
                    path1: Directory.GetCurrentDirectory().RefinePlatformPath(),
                    path2: "idc-shr-dependency",
                    path3: "appconfigs.db"
                )
                .RefinePlatformPath()
        );

        builder.Services.AddSingleton(implementationFactory: _ => _appConfigurations);
        return builder;
    }

    private static WebApplicationBuilder SetupAppSettings(this WebApplicationBuilder builder)
    {
        _appSettings = AppSettings.Load(filePath: "appsettings.json");
        builder.Services.AddSingleton(implementationFactory: _ => _appSettings);
        return builder;
    }

    private static WebApplicationBuilder SetupLanguage(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(implementationFactory: _ => new Language(
            jsonPath: Path.Combine(
                    path1: Directory.GetCurrentDirectory().RefinePlatformPath(),
                    path2: "wwwroot/messages.json".RefinePlatformPath()
                )
                .RefinePlatformPath(),
            defaultLanguage: _appConfigurations.Get(path: "Language", defaultValue: "en")!
        ));
        return builder;
    }

    private static WebApplicationBuilder SetupSystemLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(implementationFactory: _ =>
        {
            _systemLogging ??= new SystemLogging(
                options: new SystemLoggingOptions().MapProperties(
                    jObject: _appConfigurations.Get<JObject>(path: "Logging", throwOnNull: true)!
                ),
                source: CON_STR_APP_NAME
            );
            return _systemLogging;
        });

        return builder;
    }

    private static WebApplicationBuilder SetupCaching(this WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.Caching.Enable", defaultValue: false))
            builder.Services.AddSingleton(static _ => new Caching(
                defaultExpirationMinutes: _appConfigurations.Get(
                    path: "DependencyInjection.Caching.ExpirationInMinutes",
                    defaultValue: 30
                )
            ));

        return builder;
    }

    private static WebApplicationBuilder SetupPlugins(this WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "Plugins.Enable", defaultValue: false))
        {
            builder.Services.AddSingleton(implementationFactory: _ =>
            {
                var plugin = new PluginManager(systemLogging: _systemLogging);
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

        return builder;
    }

    private static WebApplicationBuilder SetupPGSQL(this WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.PGSQL", defaultValue: false))
            builder.Services.AddScoped(implementationFactory: static _ =>
            {
                var defaultConString = _appSettings.Get(
                    path: "DefaultConStrings.IDCAggrMapping.PGSQL",
                    defaultValue: "ConnectionString_en"
                );

                var ccs = new CommonConnectionString().FromConnectionString(
                    connectionString: _appSettings.Get(
                        path: $"DbContextSettings.{defaultConString}",
                        defaultValue: "User ID=idc_fadhly;Password={pass};HOST=localhost;Port=5432;Database=idc.en;Pooling=true;MinPoolSize=1;MaxPoolSize=1000;"
                    )
                );

                ccs = ccs.ChangePassword(
                    newPassword: Commons.IS_DEBUG_MODE
                        ? (ccs.Password ?? string.Empty)
                        : (
                            _appSettings.Get<string?>(path: "configPass.passwordDB")
                            ?? throw new InvalidOperationException(
                                "Failed to retrieve database password."
                            )
                        ).LegacyDecryptor(
                            key: _appSettings.Get(
                                path: "KeyConvert.DecryptionKey",
                                defaultValue: "idxpartners"
                            )
                        )
                );

                return new PostgreHelper(
                    connectionString: ccs.ToPostgreSQL(applicationName: CON_STR_APP_NAME),
                    logging: _appConfigurations.Get(
                        path: "Logging.RegisterAsDI",
                        defaultValue: true
                    )
                        ? _systemLogging
                        : null
                );
            });

        return builder;
    }

    private static WebApplicationBuilder SetupSQLite(this WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.SQLite", defaultValue: false))
            builder.Services.AddScoped(implementationFactory: static _ =>
            {
                var defaultConString = _appSettings.Get(
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

        return builder;
    }

    private static WebApplicationBuilder SetupMongoDB(this WebApplicationBuilder builder)
    {
        if (_appConfigurations.Get(path: "DependencyInjection.MongoDB", defaultValue: false))
            builder.Services.AddScoped(static _ =>
            {
                var defaultConString = _appSettings.Get(
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

        return builder;
    }

    private static WebApplicationBuilder SetupHangfire(this WebApplicationBuilder builder)
    {
        var defaultConString = _appSettings.Get(
            path: "DefaultConStrings.IDCAggrMapping.PGSQL",
            defaultValue: "ConnectionString_en"
        );

        builder.Services.AddHangfire(config =>
            config
                .SetDataCompatibilityLevel(compatibilityLevel: CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(configure: options =>
                {
                    var ccs = new CommonConnectionString().FromConnectionString(
                        connectionString: _appSettings.Get(
                            path: $"DbContextSettings.{defaultConString}",
                            defaultValue: "User ID=idc_fadhly;Password={pass};HOST=localhost;Port=5432;Database=idc.en;Pooling=true;MinPoolSize=1;MaxPoolSize=1000;"
                        )
                    );

                    ccs = ccs.ChangePassword(
                        newPassword: Commons.IS_DEBUG_MODE
                            ? (ccs.Password ?? string.Empty)
                            : (
                                _appSettings.Get<string?>(path: "configPass.passwordDB")
                                ?? throw new InvalidOperationException(
                                    "Failed to retrieve database password."
                                )
                            ).LegacyDecryptor(
                                key: _appSettings.Get(
                                    path: "KeyConvert.DecryptionKey",
                                    defaultValue: "idxpartners"
                                )
                            )
                    );

                    options.UseNpgsqlConnection(
                        connectionString: ccs.ToPostgreSQL(applicationName: CON_STR_APP_NAME)
                    );
                })
        );

        return builder;
    }

    private static WebApplicationBuilder SetupHttpClientUtility(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClientUtility(systemLogging: _systemLogging);
        return builder;
    }

    private static WebApplicationBuilder SetupApiKeyAuthentication(
        this WebApplicationBuilder builder
    )
    {
        builder.Services.AddApiKeyAuthentication(opt =>
        {
            opt.SetLogger(logger: _systemLogging)
                .MapProperties(
                    jObject: _appConfigurations.Get<JObject>(
                        path: "Middlewares.APIKey",
                        defaultValue: []
                    )!
                );

            // Buatkan logika untuk memproses API key dari sumber data
            opt.ApiKeyFetcherAsync = async (apiKey) =>
            {
                await Task.Delay(millisecondsDelay: 10);
                return true;
            };

            // Buatkan logika "TAMBAHAN" untuk memproses ketika API key tidak ditemukan
            opt.OnApiKeyNotFoundAsync = async () =>
            {
                // Catatan: Tidak perlu membuat log di sini karena sudah ada di middleware
                await Task.CompletedTask;
            };

            // Buatkan logika "TAMBAHAN" untuk memproses ketika API key tidak valid
            opt.OnUnauthorizedAsync = async () =>
            {
                // Catatan: Tidak perlu membuat log di sini karena sudah ada di middleware
                await Task.CompletedTask;
            };

            // Buatkan logika "TAMBAHAN" untuk memproses ketika API key valid
            opt.OnAuthorizedAsync = async () =>
            {
                // Boleh membuat log karena belum ada proses logging di middleware
                _systemLogging.LogInformation(message: "API key is valid");
                await Task.CompletedTask;
            };
        });
        return builder;
    }
}
