using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Comm.Http;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for managing HTTP client operations and demonstrations
/// </summary>
/// <remarks>
/// Provides endpoints for demonstrating HTTP client functionality including:
/// - Making HTTP requests
/// - Handling responses
/// - Logging request/response details
/// - Error handling
///
/// Example usage:
/// <code>
/// var controller = new DemoControllerHttpClient(
///     language: new Language(),
///     systemLogging: new SystemLogging(),
///     httpClient: new HttpClientUtility()
/// );
/// var response = await controller.Get();
/// </code>
/// </remarks>
/// <param name="language">Service for handling localization and messages</param>
/// <param name="systemLogging">Service for system logging operations</param>
/// <param name="httpClient">Utility for making HTTP requests</param>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoHttpClient(
    Language language,
    SystemLogging systemLogging,
    HttpClientUtility httpClient
) : ControllerBase
{
    /// <summary>
    /// Retrieves data from a demo endpoint using HTTP GET request
    /// </summary>
    /// <remarks>
    /// Makes a GET request to the system logging files endpoint and returns the response as a JObject.
    /// Request and response logging are enabled for this operation.
    ///
    /// Example usage:
    /// <code>
    /// var result = await controller.Get();
    /// var files = result.Data["files"];
    /// </code>
    ///
    /// > [!NOTE]
    /// > This endpoint enables both request and response logging automatically
    ///
    /// > [!IMPORTANT]
    /// > Requires proper configuration of HttpClientUtility and system logging
    /// </remarks>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> containing a <see href="https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JObject.htm">JObject</see>
    /// with the response data
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails</exception>
    /// <exception cref="JsonReaderException">Thrown when response cannot be parsed as JObject</exception>
    [Tags(tags: "HttpClient"), HttpGet(template: "get")]
    public async Task<APIResponseData<JObject>> Get()
    {
        try
        {
            httpClient.SwitchRequestLogging(enabled: true).SwitchResponseLogging(enabled: true);

            return await httpClient.GetApiResponseDataAsync<JObject>(
                uri: $"{Request.Scheme}://{Request.Host}/api/demo/DemoControllerSystemLogging/Files"
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<JObject>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
