using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.AggrMapping.Utilities.Models.AggregateEngine;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Controller for Aggregation
/// </summary>
/// <param name="caching">
///     Service for caching
/// </param>
/// <param name="pgHelper">
///     Service for handling PostgreSQL
/// </param>
/// <param name="systemLogging">
///     Service for system logging
/// </param>
[Route("AggrMapping/[controller]")]
[ApiController]
public class AggregateEngine(Caching caching, PostgreHelper pgHelper, SystemLogging systemLogging)
    : ControllerBase
{
    /// <summary>
    ///     Aggregates data
    /// </summary>
    /// <param name="data">
    ///     The data to be aggregated
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result
    /// </returns>
    [Tags(tags: "Aggregation"), HttpPost("Aggregate")]
    public async Task<APIResponseData<object?>> Aggregate(
        [FromBody] AggregateAndInsertPayloadModel data,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await data.ChangeOperationType(
                    operationType: AggregateAndInsertPayloadModel.OperationTypes.Aggregation
                )
                .Validate(
                    pgHelper: pgHelper,
                    caching: caching,
                    cancellationToken: cancellationToken
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
}
