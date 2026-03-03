using System.Data;
using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing system logging operations
/// </summary>
/// <remarks>
/// Provides endpoints for writing logs at different levels (Info, Warning, Error) and retrieving log information.
/// Supports operations like viewing log files and reading log entries within a specified time range.
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// var controller = new DemoControllerSystemLogging(
///     appConfigs: new AppConfigsHandler(),
///     language: new Language(),
///     systemLogging: new SystemLogging()
/// );
/// </code>
/// </example>
/// <param name="language">Service for handling language and localization</param>
/// <param name="systemLogging">Service for system-wide logging operations</param>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoSystemLogging(Language language, SystemLogging systemLogging) : ControllerBase
{
    private const string CON_API_STATUS_FAILED = "api.status.failed";

    /// <summary>
    /// Write logs an information message
    /// </summary>
    /// <param name="message">Message to log</param>
    [Tags(tags: "System Logging"), HttpPost(template: "Info")]
    public APIResponse LogInfo([FromBody] string message)
    {
        try
        {
            throw new DataException(s: message);
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Write logs a warning message
    /// </summary>
    /// <param name="message">Message to log</param>
    [Tags(tags: "System Logging"), HttpPost(template: "Warning")]
    public APIResponse LogWarning([FromBody] string message)
    {
        try
        {
            throw new DataException(s: message);
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Write logs an error message
    /// </summary>
    /// <param name="message">Message to log</param>
    [Tags(tags: "System Logging"), HttpPost(template: "Error")]
    public APIResponse LogError([FromBody] string message)
    {
        try
        {
            throw new DataException(s: message);
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Write logs an error with exception details
    /// </summary>
    /// <param name="message">Message to log</param>
    [Tags(tags: "System Logging"), HttpPost(template: "ErrorWithStackTrace")]
    public APIResponse LogErrorWithException([FromBody] string message)
    {
        try
        {
            throw new DataException(s: message);
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(exception: ex, logging: systemLogging, includeStackTrace: true);
        }
    }
}
