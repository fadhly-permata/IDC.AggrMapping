using System.Data;
using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Helpers;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
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
[Route(template: "AggrMapping/[controller]")]
[ApiController]
public class AggregateEngine(SystemLogging systemLogging, Caching caching, PostgreHelper pgHelper)
    : ControllerBase
{
    /// <summary>
    ///     Processes and aggregates a single data item
    /// </summary>
    /// <remarks>
    ///     This endpoint processes a single data item using the specified aggregation configuration.
    ///     The configuration is loaded based on the provided code and applied to the input data.
    ///
    ///     Sample request:
    ///     ```json
    ///     POST /AggrMapping/AggregateEngine/SingleAggregate
    ///     Content-Type: application/json
    ///
    ///     {
    ///         "batch_id": "BATCH-123",
    ///         "code": "AGG-001",
    ///         "data": {
    ///             "field1": "value1",
    ///             "field2": 100
    ///         }
    ///     }
    ///     ```
    ///
    ///     **Notes:**
    ///     - The batch_id must be unique for tracking purposes
    ///     - The code must reference an existing aggregation configuration
    ///     - The data field should contain the payload to be processed
    /// </remarks>
    /// <param name="payload">
    ///     The aggregation request payload containing:
    ///     - batch_id (string): Unique identifier for the batch
    ///     - code (string): Aggregation configuration code
    ///     - data (object): The data to be processed
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the asynchronous operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Error message if status is "Failed"
    ///     - Data: JObject containing the log entries for the configuration
    /// </returns>
    [Tags(tags: "Aggregation"), HttpPost(template: "Aggregator")]
    public async Task<APIResponseData<object?>> SingleAggregate(
        [FromBody] UpsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken = default
    )
    {
        var jqe = new JsonQueryEngine(jsonContext: (JObject)payload.Data);
        JObject result = [];
        var errMsg = string.Empty;

        try
        {
            await payload
                .ChangeOperationType(
                    operationType: UpsertAndAggregatePayloadModel.OperationTypes.Aggregation
                )
                .Validate();

            await payload.AssignNullBatchId(
                prefix: "SINGLE",
                pgHelper: pgHelper,
                cancellationToken: cancellationToken
            );

            var cfg = await new AggConfigEngineModel().Load(
                aggregateCode: payload.Code,
                pgHelper: pgHelper,
                caching: caching,
                systemLogging: systemLogging,
                cancellationToken: cancellationToken
            );

            if (cfg.Configurations is not { Count: not 0 })
                throw new DataException(s: $"Aggregate configuration '{payload.Code}' not found.");

            result = jqe.AggregateProcessorAsync(queryConfig: cfg.Configurations);
            return new APIResponseData<object?>().ChangeData(data: result);
        }
        catch (Exception ex)
        {
            errMsg = ex.GetExceptionDetails();
            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
        finally
        {
            await jqe.SaveLog(
                logData: new LogDataModel
                {
                    BatchId = payload.BatchId,
                    TotalProcess = payload.TotalProcess,
                    ProcessIndex = payload.ProcessIndex,
                    ProcessType = LogDataModel.ProcessKind.ML_AGGREGATE,
                    ProcessCode = payload.Code,
                    Request = payload.Data.ToString(),
                    Response = result.ToString(),
                    Log = errMsg,
                },
                pgHelper: pgHelper,
                cancellationToken: cancellationToken
            );
        }
    }

    /// <summary>
    ///     Processes multiple data items for aggregation based on the provided configuration.
    ///     Each item in the data array will be processed individually, and errors in one item
    ///     will not stop the processing of subsequent items.
    /// </summary>
    /// <param name="payload">
    ///     The payload containing:
    ///     - batch_id: Unique identifier for the batch (required)
    ///     - code: Aggregation configuration code (required)
    ///     - data: Array of JSON objects to be processed (required, non-empty)
    /// </param>
    /// <param name="cancellationToken">
    ///     Token to monitor for cancellation requests
    /// </param>
    /// <returns>
    ///     Returns an <see cref="APIResponseData{T}"/> containing:
    ///     - JArray of processed results for each input item
    ///     - Error details if the overall operation fails
    /// </returns>
    /// <remarks>
    ///     Sample request:
    ///     ```json
    ///     POST /api/aggregate/MultipleAggregate
    ///     {
    ///         "batch_id": "batch-123",
    ///         "code": "ACV1",
    ///         "data": [
    ///             { "field1": "value1" },
    ///             { "field1": "value2" }
    ///         ]
    ///     }
    ///     ```
    ///
    ///     Each item in the data array will be processed as follows:
    ///     1. Individual aggregation using the specified configuration code
    ///     2. Error handling per item (errors are logged but don't stop processing)
    ///     3. Detailed logging for each processed item
    /// </remarks>
    [Tags(tags: "Aggregation"), HttpPost(template: "MultipleAggregator")]
    public async Task<APIResponseData<object?>> MultipleAggregate(
        [FromBody] UpsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await payload
                .ChangeOperationType(
                    operationType: UpsertAndAggregatePayloadModel.OperationTypes.Aggregation
                )
                .Validate();

            await payload.AssignNullBatchId(
                prefix: "MULTIPLE",
                pgHelper: pgHelper,
                cancellationToken: cancellationToken
            );

            var cfg = await new AggConfigEngineModel().Load(
                aggregateCode: payload.Code,
                pgHelper: pgHelper,
                caching: caching,
                systemLogging: systemLogging,
                cancellationToken: cancellationToken
            );

            if (cfg is null || cfg.Configurations.Count == 0)
                throw new DataException(s: $"Aggregate configuration '{payload.Code}' not found.");

            var result = new JArray();
            var itemIndex = 0;
            payload.TotalProcess = ((JArray)payload.Data).Count;

            foreach (var item in (JArray)payload.Data)
            {
                itemIndex++;
                string? errorMessage = null;
                var jqe = new JsonQueryEngine(jsonContext: (JObject)item);
                JObject itemResult = [];

                try
                {
                    itemResult = jqe.AggregateProcessorAsync(queryConfig: cfg.Configurations);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                await jqe.SaveLog(
                    logData: new LogDataModel
                    {
                        BatchId = payload.BatchId,
                        ProcessIndex = itemIndex,
                        TotalProcess = payload.TotalProcess,
                        ProcessType = LogDataModel.ProcessKind.ML_AGGREGATE,
                        ProcessCode = payload.Code,
                        Request = item.ToString(formatting: Newtonsoft.Json.Formatting.None),
                        Response =
                            errorMessage == null
                                ? itemResult.ToString(formatting: Newtonsoft.Json.Formatting.None)
                                : null,
                        Log = errorMessage,
                    },
                    pgHelper: pgHelper,
                    cancellationToken: cancellationToken
                );

                result.Add(item: itemResult);
            }

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
