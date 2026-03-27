using Hangfire;
using Hangfire.Dashboard;
using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Template.ConfigAndSettings;

namespace IDC.AggrMapping;

internal static partial class Program
{
    private const string CON_STR_APP_NAME = "IDC.AggrMapping";

    private static AppConfigurations _appConfigurations = null!;
    private static AppSettings _appSettings = null!;
    private static SystemLogging? _systemLogging;

    private static string AppNameTrimmed() => CON_STR_APP_NAME.Replace(oldValue: ".", newValue: "");

    private static async Task Main(string[] args)
    {
        if (OperatingSystem.IsLinux())
            Environment.SetEnvironmentVariable(
                variable: "DOTNET_USE_POLLING_FILE_WATCHER",
                value: "1"
            );

        var builder = WebApplication
            .CreateBuilder(args: args)
            .SetupAppConfigurations()
            .SetupAppSettings()
            .SetupLanguage()
            .SetupSystemLogging()
            .SetupHttpClientUtility()
            .SetupApiKeyAuthentication()
            .SetupServices()
            .SetupSwagger()
            .SetupCaching()
            .SetupPgsql()
            .SetupSqLite()
            .SetupMongoDb()
            .SetupPlugins()
            .SetupHangfire();

        _ = await builder.SetupHangfireServer();

        var app = builder.Build();
        ConfigureMiddlewares(app: app);

        if (_appConfigurations.Get<bool>(path: "Middlewares.Cors.Enabled"))
            app.UseCors(policyName: $"{AppNameTrimmed()}-CorsPolicy");

        app.UseHangfireDashboard(
            pathMatch: $"/hangfire/{AppNameTrimmed()}",
            options: new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
                DashboardTitle = "Background Job Dashboard",
            }
        );

        await app.RunAsync();
    }

    private static async Task<WebApplicationBuilder> SetupHangfireServer(
        this WebApplicationBuilder builder
    )
    {
        using var serviceProvider = builder.Services.BuildServiceProvider();
        var caching = serviceProvider.GetRequiredService<Caching>();
        var pgHelper = serviceProvider.GetRequiredService<PostgreHelper>();

        var gcm = await new GlobalConfigurationModel().InitFromDatabase(
            pgHelper: pgHelper,
            caching: caching
        );

        builder.Services.AddHangfireServer(
            optionsAction: (_, options) =>
            {
                options.WorkerCount = gcm.MaxParallelProcess;
                options.Queues = ["high_priority", "default", "low_priority"];
            }
        );

        return builder;
    }

    // Filter untuk otorisasi dashboard
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // Allow di development
            if (Commons.IS_DEBUG_MODE)
                return true;

            // Di stage non debug, lakukan cek API key
            return context.GetHttpContext().Request.Headers[key: "X-Hangfire-Key"]
                == "your-secret-key-here";
        }
    }
}
