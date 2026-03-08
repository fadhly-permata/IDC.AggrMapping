using Hangfire;
using Hangfire.Dashboard;
using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Template.ConfigAndSettings;

namespace IDC.AggrMapping;

internal static partial class Program
{
    private const string CON_STR_APP_NAME = "IDC.AggrMapping";

    private static AppConfigurations _appConfigurations = null!;
    private static AppSettings _appSettings = null!;
    private static SystemLogging _systemLogging = null!;

    private static void Main(string[] args)
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
            .SetupPGSQL()
            .SetupSQLite()
            .SetupMongoDB()
            .SetupPlugins()
            .SetupHangfire();

        var app = builder.Build();
        ConfigureMiddlewares(app: app);

        if (_appConfigurations.Get<bool>(path: "Middlewares.Cors.Enabled"))
            app.UseCors(policyName: $"{CON_STR_APP_NAME}-CorsPolicy");

        app.UseHangfireDashboard(
            "/hangfire/" + CON_STR_APP_NAME.Replace(oldValue: ".", newValue: ""),
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
            // Allow di development
            if (Commons.IS_DEBUG_MODE)
                return true;

            // Di production, cek API key sederhana
            return context.GetHttpContext().Request.Headers["X-Hangfire-Key"]
                == "your-secret-key-here";
        }
    }
}
