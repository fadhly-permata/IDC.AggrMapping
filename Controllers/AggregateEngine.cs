using System.Data;
using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.AggrMapping.Utilities.Models.AggregateEngine;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Controller for Aggregation
/// </summary>
/// <param name="systemLogging">
///     Service for system logging
/// </param>
/// <param name="caching">
///     Service for caching
/// </param>
/// <param name="pgHelper">
///     Service for handling PostgreSQL
/// </param>
/// <remarks>
///     This controller provides endpoints for aggregating data.
/// </remarks>
[Route("AggrMapping/[controller]")]
[ApiController]
public class AggregateEngine(SystemLogging systemLogging, Caching caching, PostgreHelper pgHelper)
    : ControllerBase
{
    /// <summary>
    ///     Aggregates data
    /// </summary>
    /// <param name="payload">
    ///     The data to be aggregated
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result
    /// </returns>
    [Tags(tags: "Aggregation"), HttpPost("SingleAggregate")]
    public async Task<APIResponseData<object?>> SingleAggregate(
        [FromBody] InsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await payload
                .ChangeOperationType(
                    operationType: InsertAndAggregatePayloadModel.OperationTypes.Aggregation
                )
                .Validate();

            var cfg = await new AggregateConfigurationModel().Load(
                aggregateCode: payload.Code,
                pgHelper: pgHelper,
                caching: caching,
                cancellationToken: cancellationToken
            );

            if (cfg.Configurations == null || cfg.Configurations.Count == 0)
                throw new DataException($"Aggregate configuration '{payload.Code}' not found.");

            return new APIResponseData<object?>().ChangeData(
                data: await new JsonQueryEngine(
                    jsonContext: (JObject)payload.Data
                ).AggregateProcessorAsync(
                    queryConfig: cfg.Configurations,
                    cancellationToken: cancellationToken
                )
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
    ///     Aggregates multiple data
    /// </summary>
    /// <param name="payload">
    ///     The data to be aggregated
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result
    /// </returns>
    /// <exception cref="DataException">
    ///     Thrown when the aggregate configuration is not found
    /// </exception>
    [Tags(tags: "Aggregation"), HttpPost("MultipleAggregate")]
    public async Task<APIResponseData<object?>> MultipleAggregate(
        [FromBody] InsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await payload
                .ChangeOperationType(
                    operationType: InsertAndAggregatePayloadModel.OperationTypes.Aggregation
                )
                .Validate();

            var cfg = await new AggregateConfigurationModel().Load(
                aggregateCode: payload.Code,
                pgHelper: pgHelper,
                caching: caching,
                cancellationToken: cancellationToken
            );

            if (cfg == null || cfg.Configurations == null || cfg.Configurations.Count == 0)
                throw new DataException($"Aggregate configuration '{payload.Code}' not found.");

            var result = new JArray();
            foreach (var item in (JArray)payload.Data)
                result.Add(
                    await new JsonQueryEngine(jsonContext: (JObject)item).AggregateProcessorAsync(
                        queryConfig: cfg.Configurations,
                        cancellationToken: cancellationToken
                    )
                );

            return new APIResponseData<object?>().ChangeData(data: result);
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
