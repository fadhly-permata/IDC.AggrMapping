using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Controller for managing PostgreSQL database operations
/// </summary>
/// <param name="systemLogging">
///     Service for system logging operations
/// </param>
/// <param name="pgHelper">
///     Service for handling PostgreSQL database operations
/// </param>
/// <param name="language">
///     Service for handling language and localization
/// </param>
[Route("AggrMapping/demo/[controller]")]
[ApiController]
public class DemoPgSql(SystemLogging systemLogging, PostgreHelper pgHelper, Language language)
    : ControllerBase
{
    /// <summary>
    ///     Controller for managing PostgreSQL database operations
    /// </summary>
    /// <param name="query">
    ///     SQL query to be executed
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for asynchronous operations
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result of the query
    /// </returns>
    [HttpPost(template: "QueryTester")]
    public async Task<APIResponseData<object?>> QueryTester(
        [FromBody] string query,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
            var (_, data) = await pgHelper.ExecuteQueryAsync(
                query: query,
                callback: (Action<List<JObject>>?)null,
                cancellationToken: cancellationToken
            );
            return new APIResponseData<object?>().ChangeData(data: data);
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(exception: ex, logging: systemLogging, includeStackTrace: true);
        }
    }
}
