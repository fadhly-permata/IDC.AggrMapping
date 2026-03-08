using System.Reflection;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities.Template.Middlewares.SwaggerBehave;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace IDC.AggrMapping;

internal partial class Program
{
    private static WebApplicationBuilder SetupSwagger(this WebApplicationBuilder builder)
    {
        if (!_appConfigurations.Get<bool>(path: "SwaggerConfig.UI.Enable"))
            return builder;

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            var openApiInfo = new OpenApiInfo
            {
                Title = _appConfigurations.Get<string>(path: "SwaggerConfig.OpenApiInfo.Title")!,
                Version = _appConfigurations.Get<string>(
                    path: "SwaggerConfig.OpenApiInfo.Version"
                )!,
                Description = _appConfigurations.Get<string>(
                    path: "SwaggerConfig.OpenApiInfo.Description"
                )!,
                TermsOfService = new Uri(
                    _appConfigurations.Get<string>(
                        path: "SwaggerConfig.OpenApiInfo.TermsOfService"
                    )!,
                    UriKind.Relative
                ),
                Contact = new OpenApiContact
                {
                    Name = _appConfigurations.Get<string>(
                        path: "SwaggerConfig.OpenApiInfo.Contact.Name"
                    )!,
                    Email = _appConfigurations.Get<string>(
                        path: "SwaggerConfig.OpenApiInfo.Contact.Email"
                    )!,
                    Url = new Uri(
                        _appConfigurations.Get<string>(
                            path: "SwaggerConfig.OpenApiInfo.Contact.Url"
                        )!
                    ),
                },
                License = new OpenApiLicense
                {
                    Name = _appConfigurations.Get<string>(
                        path: "SwaggerConfig.OpenApiInfo.License.Name"
                    )!,
                    Url = new Uri(
                        _appConfigurations.Get<string>(
                            path: "SwaggerConfig.OpenApiInfo.License.Url"
                        )!,
                        UriKind.Relative
                    ),
                },
            };

            // Tambahkan resolver untuk menangani konflik action
            options.ResolveConflictingActions(apiDescriptions =>
            {
                // Prioritaskan controller dari namespace IDC.AggrMapping
                return apiDescriptions.FirstOrDefault(api =>
                        api.ActionDescriptor.DisplayName?.Contains(
                            value: _appConfigurations.Get(
                                path: "AppName",
                                defaultValue: CON_STR_APP_NAME
                            )!
                        ) == true
                    ) ?? apiDescriptions.First();
            });

            options.SwaggerDoc(name: "Main", info: openApiInfo);
            options.SwaggerDoc(
                name: "Demo",
                info: new OpenApiInfo
                {
                    Title = "Demo API",
                    Version = openApiInfo.Version,
                    Description = openApiInfo.Description,
                    TermsOfService = openApiInfo.TermsOfService,
                    Contact = openApiInfo.Contact,
                    License = openApiInfo.License,
                }
            );

            // Konfigurasi untuk mengelompokkan berdasarkan Tags
            options.TagActionsBy(api =>
                [
                    .. api
                        .ActionDescriptor.EndpointMetadata.OfType<TagsAttribute>()
                        .SelectMany(attr => attr.Tags)
                        .Distinct(),
                ]
            );

            // Urutkan Tags
            options.OrderActionsBy(apiDesc => apiDesc.GroupName);

            options.AddSecurityDefinition(
                "ApiKey",
                new OpenApiSecurityScheme
                {
                    Description = "API Key authentication using the 'X-API-Key' header",
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Scheme = "ApiKeyScheme",
                }
            );

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey",
                            },
                        },
                        Array.Empty<string>()
                    },
                }
            );

            options.DocInclusionPredicate(
                (docName, api) =>
                {
                    // Exclude endpoints from MongoDB reference DLLs
                    if (api.RelativePath != null && ExcludeAPIPath(api.RelativePath))
                        return false;

                    if (docName == "Demo")
                        return api.RelativePath?.ToLower().Contains("api/demo/") == true
                            || api.GroupName?.Equals("Demo", StringComparison.OrdinalIgnoreCase)
                                == true;

                    if (docName == "Main")
                        return api.GroupName?.Equals("Main", StringComparison.OrdinalIgnoreCase)
                                == true
                            || api.GroupName == null;

                    return true;
                }
            );
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
            options.IncludeXmlComments(xmlPath);

            options.DocumentFilter<SwaggerDocGroupByTagsFilter>();
            if (_appConfigurations.Get<bool>(path: "SwaggerConfig.UI.SortEndpoints"))
                options.DocumentFilter<SwaggerSortDocFilter>();
        });

        return builder;
    }

    private static void ConfigureSwaggerUI(WebApplication app)
    {
        if (!_appConfigurations.Get<bool>(path: "SwaggerConfig.UI.Enable"))
            return;

        app.UseSwagger();
        app.UseSwaggerUI(setupAction: options =>
        {
            // Main endpoints
            ConfigureMainEndpoint(options: options);

            // Demo endpoints
            ConfigureDemoEndpoint(options: options);

            // Additional endpoints from SwaggerList
            ConfigureAdditionalEndpoints(options: options, app: app);

            ConfigureSwaggerUIStyle(options: options);
        });
    }

    private static void ConfigureMainEndpoint(SwaggerUIOptions options)
    {
        options.SwaggerEndpoint(
            url: "/swagger/Main/swagger.json",
            name: $"{CON_STR_APP_NAME} - API Docs"
        );
    }

    private static void ConfigureDemoEndpoint(SwaggerUIOptions options)
    {
        options.SwaggerEndpoint(
            url: "/swagger/Demo/swagger.json",
            name: "IDC.AggrMapping Demo API"
        );
    }

    private static void ConfigureAdditionalEndpoints(SwaggerUIOptions options, WebApplication app)
    {
        var swaggerList = app
            .Configuration.GetSection(key: "SwaggerList")
            .Get<List<SwaggerEndpoint>>()
            ?.Where(predicate: endpoint =>
                endpoint.Name != $"{CON_STR_APP_NAME} - API Docs"
                && endpoint.Name != "IDC.AggrMapping Demo API"
            )
            .OrderBy(keySelector: endpoint => endpoint.Name);

        if (swaggerList != null)
            foreach (var endpoint in swaggerList)
                options.SwaggerEndpoint(url: endpoint.URL, name: endpoint.Name);
    }

    private static void ConfigureSwaggerUIStyle(SwaggerUIOptions options)
    {
        options.DocumentTitle = $"[SUI] {CON_STR_APP_NAME} - API Docs";

        options.InjectStylesheet(
            path: _appConfigurations.Get(
                path: "SwaggerConfig.UI.Theme",
                defaultValue: "/themes/theme-monokai-dark.css"
            )!
        );
        options.InjectStylesheet(path: "/_content/IDC.AggrMapping/css/swagger-custom.css");

        options.HeadContent =
            @"
                <link rel='stylesheet' type='text/css' href='/css/swagger-custom.css' />
            ";

        options.InjectJavascript(path: "/js/swagger-theme-switcher.js");
    }

    private static bool ExcludeAPIPath(string path) =>
        _appConfigurations
            .Get<List<string>>(path: "SwaggerConfig.ExcludedPaths", defaultValue: [])
            ?.Any(predicate: excluded =>
                path.StartsWith(value: excluded, comparisonType: StringComparison.OrdinalIgnoreCase)
            ) ?? false;
}
