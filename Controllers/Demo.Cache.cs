using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing cache operations
/// </summary>
/// <remarks>
/// Provides endpoints for managing cached data including:
/// - Getting cached values
/// - Setting new cache entries
/// - Updating existing cache entries
/// - Removing cache entries
/// - Retrieving all cached values
/// </remarks>
/// <param name="language">Service for handling localization and messages</param>
/// <param name="systemLogging">Service for system logging operations</param>
/// <param name="cache">Service for caching operations</param>
/// <example>
/// <code>
/// var controller = new DemoControllerCache(
///     language: new Language(),
///     systemLogging: new SystemLogging(),
///     cache: new Caching()
/// );
/// var response = controller.Get(key: "myKey");
/// </code>
/// </example>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoCache(Language language, SystemLogging systemLogging, Caching cache)
    : ControllerBase
{
    private const string CON_API_STATUS_FAILED = "api.status.failed";

    /// <summary>
    /// Gets a cached value by key, if not exists creates new one
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache if key not exists</param>
    /// <returns>Cached value</returns>
    [Tags(tags: "Caches"), HttpPost(template: "{key}")]
    public APIResponseData<object?> Get([FromRoute] string key, [FromBody] object? value)
    {
        try
        {
            return new APIResponseData<object?>().ChangeData(
                data: cache?.Get<object?>(key: key) ?? cache?.Set(key: key, value: value)
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value if exists</returns>
    [Tags(tags: "Caches"), HttpGet(template: "{key}")]
    public APIResponseData<object?> Get([FromRoute] string key)
    {
        try
        {
            return new APIResponseData<object?>().ChangeData(data: cache?.Get<object?>(key: key));
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Updates or creates a cached value
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <returns>True if operation successful</returns>
    [Tags(tags: "Caches"), HttpPut(template: "{key}")]
    public APIResponseData<bool> Upsert([FromRoute] string key, [FromBody] object value)
    {
        try
        {
            return new APIResponseData<bool>().ChangeData(
                data: cache?.Set(key: key, value: value) != null
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
    /// Removes a cached value
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <returns>True if removal successful</returns>
    [Tags(tags: "Caches"), HttpDelete(template: "{key}")]
    public APIResponse Remove([FromRoute] string key)
    {
        try
        {
            cache?.Remove(key: key);
            return new APIResponse();
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
    /// Gets all cached key-value pairs that haven't expired
    /// </summary>
    /// <returns>Dictionary of cache keys and their values</returns>
    [Tags(tags: "Caches"), HttpGet()]
    public APIResponseData<Dictionary<string, object?>> GetAll()
    {
        try
        {
            return new APIResponseData<Dictionary<string, object?>>().ChangeData(
                data: cache?.GetAll() ?? []
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<Dictionary<string, object?>>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
