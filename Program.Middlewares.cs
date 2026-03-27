using IDC.AggrMapping.Utilities;
using IDC.Utilities.Template.Middlewares.Logging;
using IDC.Utilities.Template.Middlewares.Performances;
using IDC.Utilities.Template.Middlewares.Securities;
using IDC.Utilities.Template.Middlewares.Securities.ApiKey;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping;

internal partial class Program
{
    private static void ConfigureMiddlewares(WebApplication app)
    {
        app.UseMiddleware<ApiKeyAuthentication>()
            .UseExceptionMiddleware(isDebugMode: Commons.IS_DEBUG_MODE)
            .UseHttpRequestRateLimiter(configureOptions: static opt =>
            {
                opt.SetLogger(logger: _systemLogging)
                    .MapProperties(
                        jObject: _appConfigurations.Get<JObject>(
                            path: "Middlewares.RateLimiting",
                            defaultValue: []
                        )!
                    );

                if (opt is { Enabled: true, MaxRequests: <= 0 })
                    opt.MaxRequests = 100;
            })
            .UseHttpResponseCompression(
                enableCompression: _appConfigurations.Get(
                    path: "Middlewares.ResponseCompression",
                    defaultValue: true
                )
            )
            .UseHttpRequestLogging(
                enabled: _appConfigurations.Get(
                    path: "Middlewares.RequestLogging",
                    defaultValue: true
                )
            )
            .UseHttpAdditionalHeaders(configureOptions: static opt =>
            {
                opt.SetLogger(logger: _systemLogging)
                    .MapProperties(
                        jObject: _appConfigurations.Get<JObject>(
                            path: "Middlewares.AdditionalHeader",
                            defaultValue: []
                        )!
                    );
            })
            .UseHttpCors(configureOptions: static opt =>
            {
                opt.SetLogger(logger: _systemLogging)
                    .MapProperties(
                        jObject: _appConfigurations.Get<JObject>(
                            path: "Middlewares.Cors",
                            defaultValue: []
                        )!
                    );
            });

        app.UseHttpsRedirection();
        ConfigureSwaggerUI(app: app);
        ConfigureStaticFiles(app: app);
        app.UseAuthorization();
        app.MapControllers();
    }

    private static void ConfigureStaticFiles(WebApplication app) =>
        app.UseStaticFiles(
            options: new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    root: Path.Combine(path1: Directory.GetCurrentDirectory(), path2: "wwwroot")
                ),
                RequestPath = "",
            }
        );
}
