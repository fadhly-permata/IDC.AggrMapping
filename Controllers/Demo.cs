using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Models.API;
using IDC.Utilities.Plugins;
using IDC.Utilities.Template.ConfigAndSettings;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing demo operations
/// </summary>
/// <remarks>
/// Provides endpoints for system logging and other demo functionalities
/// </remarks>
/// <example>
/// <code>
/// var controller = new DemoController(new SystemLogging());
/// controller.LogInfo(message: "Test message");
/// </code>
/// </example>
[Route("api/demo/Managements")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class Demo(
    SystemLogging systemLogging,
    Language language,
    AppConfigurations appConfigs,
    PluginManager plugin
) : ControllerBase
{
    /// <summary>
    /// Retrieves all currently loaded plugin instances.
    /// </summary>
    /// <remarks>
    /// This endpoint returns a list of all active plugins by instantiating each loaded plugin type.
    /// </remarks>
    /// <code>
    /// GET /api/demo/plugins/Active
    /// </code>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> containing the list of active plugin instances.
    /// </returns>
    /// <remarks>
    /// The returned data is a list of <see cref="IPlugin"/> instances.
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown when an error occurs during plugin instantiation or retrieval.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.GetAllPlugins();
    /// </code>
    /// </example>
    /// > [!NOTE]
    /// > Only plugins that can be instantiated and implement <see cref="IPlugin"/> are included.
    [Tags(tags: "Plugins Management"), HttpGet(template: "ListActivePlugins")]
    public APIResponseData<List<IPlugin>?> ListActivePlugins()
    {
        try
        {
            return new APIResponseData<List<IPlugin>?>()
                .ChangeMessage(message: "Active plugins retrieved successfully.")
                .ChangeData(data: plugin.ActivePlugins);
        }
        catch (Exception ex)
        {
            return new APIResponseData<List<IPlugin>?>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Restarts the specified plugin by reloading its instance from the source code directory.
    /// </summary>
    /// <remarks>
    /// This endpoint reloads the plugin instance using the provided <paramref name="pluginName"/>.
    /// </remarks>
    /// <code>
    /// POST /api/demo/plugins/Restart/{pluginId}
    /// </code>
    /// <param name="pluginName">
    /// The unique identifier of the plugin to be restarted.
    /// </param>
    /// <returns>
    /// <see cref="APIResponse"/> indicating the success or failure of the plugin reload operation.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when an error occurs during plugin reloading.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.RestartPlugin(pluginId: "hello_world");
    /// </code>
    /// </example>
    /// > [!IMPORTANT]
    /// > The plugin source code must exist in the 'wwwroot/plugins/source' directory.
    [Tags(tags: "Plugins Management"), HttpPost(template: "Restart/{pluginName}")]
    public APIResponse RestartPlugin(string pluginName)
    {
        try
        {
            plugin.Deactivation(pluginName: pluginName);
            plugin.Activation(pluginName: pluginName);
            return new APIResponse().ChangeMessage(
                message: $"Plugin '{pluginName}' reloaded successfully."
            );
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Adds a new plugin using the provided source code.
    /// </summary>
    /// <remarks>
    /// This endpoint allows dynamic addition of a plugin by supplying its source code.
    /// </remarks>
    /// <code>
    /// POST /api/demo/plugins/AddPlugin
    /// Body: "public class MyPlugin : IPlugin { ... }"
    /// </code>
    /// <param name="sourceCode">
    /// The C# source code of the plugin to be added.
    /// </param>
    /// <returns>
    /// <see cref="APIResponse"/> indicating the success or failure of the plugin addition.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when an error occurs during plugin addition.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.AddPlugin(sourceCode: "public class MyPlugin : IPlugin { ... }");
    /// </code>
    /// </example>
    /// > [!IMPORTANT]
    /// > The plugin source code must be valid and compatible with the plugin system.
    [Tags(tags: "Plugins Management"), HttpPost(template: "AddPlugin")]
    public APIResponse AddPlugin([FromBody] string sourceCode)
    {
        try
        {
            plugin.StoreNewPlugin(sourceCode: sourceCode, subDirectory: "", activate: true);
            return new APIResponse().ChangeMessage(message: "Plugin added successfully.");
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Updates the source code of an existing plugin and reloads its instance.
    /// </summary>
    /// <remarks>
    /// This endpoint allows updating the source code of a plugin identified by <paramref name="pluginName"/>
    /// and reloads the plugin instance from the source directory.
    /// </remarks>
    /// <code>
    /// POST /api/demo/plugins/UpdateSource/{pluginId}
    /// Body: "public class UpdatedPlugin : IPlugin { ... }"
    /// </code>
    /// <param name="pluginName">
    /// The unique identifier of the plugin to update.
    /// </param>
    /// <param name="newSourceCode">
    /// The new C# source code for the plugin.
    /// </param>
    /// <returns>
    /// <see cref="APIResponse"/> indicating the success or failure of the update operation.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when an error occurs during plugin source update or reload.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.UpdatePluginSource(pluginId: "hello_world", newSourceCode: "public class ...");
    /// </code>
    /// </example>
    /// > [!IMPORTANT]
    /// > The plugin source code must be valid and compatible with the plugin system.
    [Tags("Plugins Management"), HttpPost(template: "UpdateSource/{pluginName}")]
    public APIResponse UpdatePluginSource(string pluginName, [FromBody] string newSourceCode)
    {
        try
        {
            plugin.SetSourceCode(pluginName: pluginName, sourceCode: newSourceCode);
            return new APIResponse().ChangeMessage(
                message: $"Plugin '{pluginName}' source updated successfully."
            );
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Retrieves the source code of a specified plugin by its unique identifier.
    /// </summary>
    /// <remarks>
    /// This endpoint returns the C# source code file for the plugin identified by <paramref name="pluginName"/>.
    /// </remarks>
    /// <code>
    /// GET /api/demo/plugins/GetSource/{pluginId}
    /// </code>
    /// <param name="pluginName">
    /// The unique identifier of the plugin whose source code is to be retrieved.
    /// </param>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> containing the plugin's source code as a string.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the plugin is not registered or its source file cannot be found.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when an error occurs while reading the source file.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.GetPluginSource(pluginId: "hello_world");
    /// </code>
    /// </example>
    /// > [!IMPORTANT]
    /// > The plugin source file must exist in the configured source directory.
    [Tags("Plugins Management"), HttpGet(template: "GetSource/{pluginName}")]
    public APIResponseData<string> GetPluginSource(string pluginName)
    {
        try
        {
            return new APIResponseData<string>()
                .ChangeMessage(message: $"Plugin '{pluginName}' source retrieved successfully.")
                .ChangeData(data: plugin.GetSourceCode(pluginName: pluginName));
        }
        catch (Exception ex)
        {
            return new APIResponseData<string>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Updates or retrieves the plugin configuration section in the application configuration.
    /// </summary>
    /// <remarks>
    /// This endpoint allows updating the 'Plugins' configuration section with the provided JSON object.
    /// If the request body is null, it returns the current plugin configuration.
    /// </remarks>
    /// <code>
    /// POST /api/demo/plugins/Configurations
    /// Body: { "EnabledPlugins": ["hello_world", "call_other"] }
    /// </code>
    /// <param name="config">
    /// The <see cref="JObject"/> containing the plugin configuration to update.
    /// If null, the current configuration is returned.
    /// </param>
    /// <returns>
    /// <see cref="APIResponseData{JObject}"/> containing the updated or current plugin configuration.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when an error occurs during configuration update or plugin registration.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = controller.ConfigurationManager(config: new JObject { ... });
    /// </code>
    /// </example>
    /// > [!NOTE]
    /// > This endpoint updates the plugin configuration and reloads enabled plugins.
    [Tags("Plugins Management"), HttpPost(template: "Configurations")]
    public APIResponseData<JObject?> ConfigurationManager([FromBody] JObject? config)
    {
        try
        {
            if (config is null || config.Count == 0)
                return new APIResponseData<JObject?>().ChangeData(
                    data: appConfigs.Get<JObject?>(path: "Plugins", defaultValue: [])
                );

            appConfigs.Set(path: "Plugins", value: config);

            if (appConfigs.Get(path: "Plugins.Enable", defaultValue: false))
                plugin.Reinitialize(
                    pluginNames: appConfigs.Get<string[]>(
                        path: "Plugins.EnabledPlugins",
                        defaultValue: ["All"]
                    )
                );

            return new APIResponseData<JObject?>()
                .ChangeMessage(message: "Plugin configuration updated successfully.")
                .ChangeData(data: config);
        }
        catch (Exception ex)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
