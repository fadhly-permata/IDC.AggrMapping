using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for handling multi-layer adapter operations
/// </summary>
[Route("AggrMapping/[controller]")]
[ApiController]
public partial class InsertDataEngine(Caching caching, SystemLogging systemLogging) : ControllerBase
{
    /// <summary>
    ///     Inserts data into the database
    /// </summary>
    /// <param name="data">
    ///     The data to be inserted
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result
    /// </returns>
    [Tags(tags: "Insert Data"), HttpPost("InsertData")]
    public async Task<APIResponseData<object?>> InsertData(
        [FromBody] MlaPayloadModel data,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var globalConfig = await GetGlobalConfigurations(caching: caching);

            data.Validate(
                configs: new MlaPayloadModel.MlaConfigs(
                    MaxMapCount: globalConfig.MaxMapCount,
                    MaxDataCount: globalConfig.MaxDataPayload
                )
            );

            return new APIResponseData<object?>().ChangeData(data: null);
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

    [HttpDelete]
    public IActionResult DeleteData()
    {
        try
        {
            return Ok(new APIResponseData<object?>().ChangeData(data: null));
        }
        catch (Exception ex)
        {
            return BadRequest(
                new APIResponseData<object?>()
                    .ChangeStatus(status: "Failed")
                    .ChangeMessage(
                        exception: ex,
                        logging: systemLogging,
                        includeStackTrace: Commons.IS_DEBUG_MODE
                    )
            );
        }
    }
}
