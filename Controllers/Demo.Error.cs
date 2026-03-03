using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller that demonstrates various HTTP error responses.
/// </summary>
/// <sample>
/// <code>
/// GET /api/demo/error?no=404
/// </code>
/// </sample>
/// <remarks>
/// This controller uses primary constructor injection for SystemLogging.
/// </remarks>
[Route("api/demo/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Demo")]
public class DemoError : ControllerBase
{
    /// <summary>
    /// Demonstrates various HTTP error responses
    /// </summary>
    /// <param name="errMessage">Optional custom error message</param>
    /// <param name="httpStatusCode">The HTTP status code to return (0 or 200 for success)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with appropriate HTTP status code and response data</returns>
    [Tags(tags: "Error"), HttpGet("ExceptionDemo")]
    public async Task<APIResponseData<object?>> GetError(
        [FromQuery(Name = "msg")] string errMessage,
        [FromQuery(Name = "no")] int httpStatusCode = 0,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(millisecondsDelay: 10, cancellationToken: cancellationToken);

        // Tentukan jenis exception berdasarkan errorNo
        Exception ex = httpStatusCode switch
        {
            400 => new BadHttpRequestException(errMessage ?? "Bad request"),
            401 => new UnauthorizedAccessException(errMessage ?? "Unauthorized"),
            403 => new UnauthorizedAccessException(errMessage ?? "Forbidden"),
            404 => new KeyNotFoundException(errMessage ?? "Not found"),
            409 => new InvalidOperationException(errMessage ?? "Conflict"),
            _ => new InvalidOperationException(errMessage ?? "Error occurred"),
        };

        throw ex;
    }
}
