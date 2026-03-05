using Hangfire;
using Hangfire.Dashboard;
using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Comm.Http;
using IDC.Utilities.Extensions;
using IDC.Utilities.Template.ConfigAndSettings;
using IDC.Utilities.Template.Middlewares.Securities.ApiKey;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping;

internal static partial class Program
{
    private const string CON_STR_APP_NAME = "IDC.AggrMapping";

    private static AppConfigurations _appConfigurations = null!;
    private static AppSettings _appSettings = null!;
    private static SystemLogging _systemLogging = null!;
    private static Language _language = null!;

    private static void Main(string[] args)
    {
        // Increase file watcher limit for Linux systems
        if (OperatingSystem.IsLinux())
            Environment.SetEnvironmentVariable(
                variable: "DOTNET_USE_POLLING_FILE_WATCHER",
                value: "1"
            );

        var builder = WebApplication.CreateBuilder(args: args);

        // Register first most mandatory configurations
        ConfigureAppConfigurations(builder: builder);
        ConfigureAppSettings(builder: builder);
        ConfigureLanguage(builder: builder);
        ConfigureSystemLogging(builder: builder);

        // Register other objects Filters
        builder.Services.AddHttpClientUtility(systemLogging: _systemLogging);
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

        ConfigureServices(builder: builder);
        ConfigureSwagger(builder: builder);
        ConfigureCaching(builder: builder);
        ConfigurePGSQL(builder: builder);
        ConfigureSQLite(builder: builder);
        ConfigureMongoDB(builder: builder);
        ConfigurePlugins(builder: builder, systemLogging: _systemLogging);
        ConfigureHangfire(builder: builder);

        var app = builder.Build();
        ConfigureMiddlewares(app: app);

        if (_appConfigurations.Get<bool>(path: "Middlewares.Cors.Enabled"))
            app.UseCors(policyName: $"{CON_STR_APP_NAME}-CorsPolicy");

        app.UseHangfireDashboard(
            "/hangfire",
            new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
                DashboardTitle = "Background Job Dashboard",
            }
        );

        app.Run();
    }

    // Filter untuk otorisasi dashboard
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow di development
            if (Commons.IS_DEBUG_MODE)
                return true;

            // Di production, cek API key sederhana
            return httpContext.Request.Headers["X-Hangfire-Key"] == "your-secret-key-here";
        }
    }

    private static void ConfigureAppConfigurations(WebApplicationBuilder builder)
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
    }

    private static void ConfigureAppSettings(WebApplicationBuilder builder)
    {
        _appSettings = AppSettings.Load(filePath: "appsettings.json");
        builder.Services.AddSingleton(implementationFactory: _ => _appSettings);
    }

    private static void ConfigureLanguage(WebApplicationBuilder builder)
    {
        _language = new Language(
            jsonPath: Path.Combine(
                    path1: Directory.GetCurrentDirectory().RefinePlatformPath(),
                    path2: "wwwroot/messages.json".RefinePlatformPath()
                )
                .RefinePlatformPath(),
            defaultLanguage: _appConfigurations.Get(path: "Language", defaultValue: "en")!
        );

        builder.Services.AddSingleton(implementationFactory: _ => _language);
    }

    private static void ConfigureSystemLogging(WebApplicationBuilder builder) =>
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
}
