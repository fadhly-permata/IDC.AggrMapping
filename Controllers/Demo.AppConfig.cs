using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Models.API;
using IDC.Utilities.Template.ConfigAndSettings;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing demo app configuration operations
/// </summary>
/// <param name="systemLogging">
/// Service for system logging operations
/// </param>
/// <param name="appConfigs">
/// Service for handling app configurations
/// </param>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoAppConfig(SystemLogging systemLogging, AppConfigurations appConfigs)
    : ControllerBase
{
    /// <summary>
    /// Controller for managing demo app configuration operations
    /// </summary>
    /// <param name="path">
    /// Path to the configuration value
    /// </param>
    /// <returns>
    /// Configuration value
    /// </returns>
    [Tags("App Config"), HttpGet(template: "")]
    public APIResponseData<object?> GetAppConfig(string path)
    {
        try
        {
            return new APIResponseData<object?>().ChangeData(
                data: appConfigs.Get<object?>(path: path)
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Controller for managing demo app configuration operations
    /// </summary>
    /// <param name="path">
    /// Path to the configuration value
    /// </param>
    /// <param name="value">
    /// Configuration value
    /// </param>
    /// <returns>
    /// Configuration value
    /// </returns>
    [Tags("App Config"), HttpPost(template: "")]
    public APIResponseData<bool> SetAppConfig(string path, object value)
    {
        try
        {
            appConfigs.Set(path: path, value: value);
            return new APIResponseData<bool>().ChangeData(data: true);
        }
        catch (Exception ex)
        {
            return new APIResponseData<bool>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
