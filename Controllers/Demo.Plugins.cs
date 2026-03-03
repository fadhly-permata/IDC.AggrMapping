using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.API;
using IDC.Utilities.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing plugin operations such as execution, addition, update, and retrieval.
/// </summary>
/// <remarks>
/// Provides endpoints for interacting with plugins in the system, including executing, adding,
/// updating, restarting, and listing plugins.
/// </remarks>
/// <example>
/// <code>
/// // Example usage:
/// var controller = new DemoPluginsController(language, systemLogging, pluginManager);
/// </code>
/// </example>
/// > [!IMPORTANT]
/// > All plugin management endpoints are grouped under 'Demo' for Swagger documentation.
[ApiController]
[Route("api/demo/plugins")]
[ApiExplorerSettings(GroupName = "Demo")]
public partial class DemoPluginsController(
    Language language,
    SystemLogging systemLogging,
    PluginManager plugin
) : ControllerBase
{
    /// <summary>
    /// Executes the Hello World plugin with the provided request context.
    /// </summary>
    /// <remarks>
    /// This endpoint invokes the 'hello_world' plugin and returns its execution result.
    /// </remarks>
    /// <code>
    /// POST /api/demo/plugins/HelloWorld
    /// Body: "Sample request"
    /// </code>
    /// <param name="request">
    /// The request context string to be passed to the Hello World plugin.
    /// </param>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> containing the plugin execution result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plugin is not registered or execution returns null.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.HelloWorld(request: "Hello");
    /// </code>
    /// </example>
    /// > [!NOTE]
    /// > This endpoint is for demonstration purposes and requires the Hello World plugin to be registered.
    [Tags(tags: "Plugins Apps"), HttpPost(template: "HelloWorld")]
    public APIResponseData<string?> HelloWorld(string request)
    {
        try
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var pluginObj =
                plugin.GetActive(pluginName: "Hello World")
                ?? throw new InvalidOperationException("Hello World plugin is not registered.");

            pluginObj.Initialize(
                // Untuk menjaga performa kirimkan yang diperlukan saja
                PluginManager: plugin,
                SystemLogging: systemLogging,
                Language: language,
                Cache: null,
                HttpClient: null,
                SQLiteHelper: null,
                PostgreHelper: null,
                MongoDatabase: null
            );

            var result =
                pluginObj.Execute(context: request)
                ?? throw new InvalidOperationException(message: "Plugin execution returned null.");

            return new APIResponseData<string?>()
                .ChangeMessage(message: "Hello World plugin executed successfully.")
                .ChangeData(data: result.ToString());
        }
        catch (Exception ex)
        {
            return new APIResponseData<string?>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Executes the Call Other plugin with the provided request context.
    /// </summary>
    /// <remarks>
    /// This endpoint invokes the 'call_other' plugin and returns its execution result.
    ///
    /// > [!NOTE]
    /// > This endpoint is for demonstration purposes and requires the Call Other plugin to be registered.
    ///
    /// <code>
    ///     POST /api/demo/plugins/CallOther
    ///     Body: "Sample request"
    /// </code>
    /// <code>
    ///     // Example request body 1:
    ///     {
    ///       "pluginName": "Hello World",
    ///       "pluginData": "John Doe"
    ///     }
    ///
    ///     // Example request body 2:
    ///     {
    ///       "pluginName": "Hitung Luas Segitiga",
    ///       "pluginData": {
    ///         "alas": 15,
    ///         "tinggi": 20
    ///       }
    ///     }
    /// </code>
    /// </remarks>
    /// <param name="request">
    /// The request context string to be passed to the Call Other plugin.
    /// </param>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> containing the plugin execution result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plugin is not registered or execution returns null.
    /// </exception>
    [Tags(tags: "Plugins Apps"), HttpPost(template: "CallOther")]
    public APIResponseData<object?> CallOther(object request)
    {
        try
        {
            request.ThrowIfNull(paramName: nameof(request), message: "Request cannot be null.");

            return new APIResponseData<object?>()
                .ChangeMessage(message: "Call Other plugin executed successfully.")
                .ChangeData(
                    data: (object?)(
                        (
                            plugin.GetActive(pluginName: "Call Other")
                            ?? throw new InvalidOperationException(
                                message: "Call Other plugin is not registered."
                            )
                        )
                            // Untuk menjaga performa kirimkan yang diperlukan saja
                            .Initialize(
                                PluginManager: plugin,
                                SystemLogging: systemLogging,
                                Language: language,
                                Cache: null,
                                HttpClient: null,
                                SQLiteHelper: null,
                                PostgreHelper: null,
                                MongoDatabase: null
                            )
                            .Execute(context: request)
                        ?? throw new InvalidOperationException(
                            message: "Plugin execution returned null."
                        )
                    )
                );
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Executes the Call Other plugin asynchronously with the provided request context.
    /// </summary>
    /// <remarks>
    /// This asynchronous endpoint invokes the 'call_other' plugin and returns its execution result.
    /// </remarks>
    /// <param name="request">
    /// The request context object to be passed to the Call Other plugin.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> containing the plugin execution result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plugin is not registered or execution returns null.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = await controller.CallOtherAsync(request: new { pluginName = "Hello World", pluginData = "John" });
    /// </code>
    /// </example>
    /// > [!NOTE]
    /// > This endpoint is for demonstration purposes and requires the Call Other plugin to be registered.
    [Tags(tags: "Plugins Apps"), HttpPost(template: "CallOtherAsync")]
    public async Task<APIResponseData<object?>> CallOtherAsync(
        object request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            request.ThrowIfNull(paramName: nameof(request), message: "Request cannot be null.");

            return new APIResponseData<object?>()
                .ChangeMessage(message: "Call Other plugin executed successfully.")
                .ChangeData(
                    data: await (
                        (
                            plugin.GetActive(pluginName: "Call Other")
                            ?? throw new InvalidOperationException(
                                message: "Call Other plugin is not registered."
                            )
                        )
                            // Untuk menjaga performa kirimkan yang diperlukan saja
                            .Initialize(
                                PluginManager: plugin,
                                SystemLogging: systemLogging,
                                Language: language,
                                Cache: null,
                                HttpClient: null,
                                SQLiteHelper: null,
                                PostgreHelper: null,
                                MongoDatabase: null
                            )
                            .ExecuteAsync(context: request, cancellationToken: cancellationToken)
                        ?? throw new InvalidOperationException(
                            message: "Plugin execution returned null."
                        )
                    )
                );
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
