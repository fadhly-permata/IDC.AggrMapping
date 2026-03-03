using IDC.Utilities.Models.API;
using IDC.Utilities.Template.Middlewares.Validations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IDC.AggrMapping;

internal partial class Program
{
    private static void ConfigureServices(WebApplicationBuilder builder)
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
                    name: $"{CON_STR_APP_NAME}-CorsPolicy",
                    configurePolicy: builder => { }
                );
            });
        }
    }
}
