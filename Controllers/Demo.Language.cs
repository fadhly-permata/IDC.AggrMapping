using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing language-related operations
/// </summary>
/// <remarks>
/// Provides endpoints for retrieving and managing language messages and configurations.
/// Supports operations like getting available languages, retrieving localized messages,
/// updating messages, and reloading language configurations.
/// </remarks>
/// <example>
/// <code>
/// var controller = new DemoControllerLanguage(language: languageService, systemLogging: loggingService);
/// var response = controller.Get(); // Gets all available languages
/// </code>
/// </example>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public partial class DemoLanguage(Language language, SystemLogging systemLogging) : ControllerBase
{
    private const string CON_API_STATUS_FAILED = "api.status.failed";

    /// <summary>
    /// Gets all available languages
    /// </summary>
    /// <returns>Array of language codes</returns>
    [Tags(tags: "Languages"), HttpGet()]
    public APIResponseData<string[]> Get()
    {
        try
        {
            return new APIResponseData<string[]>().ChangeData(
                data: language.GetAvailableLanguages()
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<string[]>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Gets a message by path and language
    /// </summary>
    /// <param name="path">Message path in dot notation</param>
    /// <param name="lang">Optional language code</param>
    /// <returns>Localized message</returns>
    [Tags(tags: "Languages"), HttpGet(template: "{lang}/{path}")]
    public APIResponseData<string> GetMessage(
        [FromRoute] string path,
        [FromRoute] string? lang = null
    )
    {
        try
        {
            return new APIResponseData<string>().ChangeData(
                data: language.GetMessage(path: path, language: lang)
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<string>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Updates or adds a message
    /// </summary>
    /// <param name="lang">Language code</param>
    /// <param name="path">Message path</param>
    /// <param name="value">New message value</param>
    [Tags(tags: "Languages"), HttpPut(template: "{lang}/{path}")]
    public APIResponseData<bool> UpdateMessage(
        [FromRoute] string lang,
        [FromRoute] string path,
        [FromBody] string value
    )
    {
        try
        {
            return new APIResponseData<bool>().ChangeData(
                data: language.UpdateMessage(language: lang, path: path, value: value)
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<bool>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Reloads messages from file
    /// </summary>
    [Tags(tags: "Languages"), HttpPost(template: "reload")]
    public APIResponseData<bool> Reload()
    {
        try
        {
            return new APIResponseData<bool>().ChangeData(data: language.Reload());
        }
        catch (Exception ex)
        {
            return new APIResponseData<bool>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
