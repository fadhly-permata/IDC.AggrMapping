using System.Data;
using System.Text;
using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Upsert Data
/// </summary>
[Route(template: "AggrMapping/[controller]")]
[ApiController]
public class InsertEngine(Caching caching, PostgreHelper pgHelper, SystemLogging systemLogging)
    : ControllerBase
{
    /// <summary>
    ///     Processes and performs UPSERT operations for data items based on mapping configurations
    /// </summary>
    /// <remarks>
    ///     This endpoint processes multiple data items and performs UPSERT operations
    ///     according to the specified mapping configurations. Each item in the data array
    ///     will be processed against all specified mapping configurations.
    ///
    ///     Sample request:
    ///     ```json
    ///     POST /AggrMapping/InsertEngine/Upsert
    ///     Content-Type: application/json
    ///
    ///     {
    ///         "batch_id": "BATCH-123",
    ///         "code": "MAP-001,MAP-002",
    ///         "data": [
    ///             {
    ///                 "field1": "value1",
    ///                 "field2": 100
    ///             },
    ///             {
    ///                 "field1": "value2",
    ///                 "field2": 200
    ///             }
    ///         ]
    ///     }
    ///     ```
    ///
    ///     **Notes:**
    ///     - Multiple mapping codes can be specified, separated by commas
    ///     - The data field must contain a JSON array of objects to be processed
    ///     - Each mapping configuration must be pre-configured in the system
    ///     - Errors in processing one mapping configuration won't affect others
    /// </remarks>
    /// <param name="payload">
    ///     The upsert request payload containing:
    ///     - batch_id (string): Unique identifier for the batch (required)
    ///     - code (string): Comma-separated mapping configuration codes (required)
    ///     - data (array): Array of JSON objects to be processed (required, non-empty)
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the asynchronous operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" if all operations completed (regardless of individual item results)
    ///     - Message: Error message if status is "Failed"
    ///     - Data: null (detailed results are logged in the database)
    /// </returns>
    [Tags(tags: "Upsert Data"), HttpPost(template: "Upsert")]
    public async Task<APIResponseData<object?>> Upsert(
        [FromBody] UpsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken = default
    )
    {
        var mapCodes = payload.Code.Split(separator: ',');
        JArray dataArray = [];

        try
        {
            await payload
                .ChangeOperationType(
                    operationType: UpsertAndAggregatePayloadModel.OperationTypes.InsertData
                )
                .Validate(
                    gcm: (GlobalConfigurationModel?)
                        await new GlobalConfigurationModel().GetGlobalConfigurationsAsync(
                            pgHelper: pgHelper,
                            caching: caching,
                            cancellationToken: cancellationToken
                        )
                );

            // Pastikan payload.Data adalah JArray
            dataArray =
                payload.Data as JArray
                ?? throw new InvalidOperationException(
                    message: "Payload data must be a JSON array."
                );

            await Task.WhenAll(
                tasks: mapCodes
                    .Select(selector: mapCode =>
                        ProcessMapCodeAsync(
                            mapCode: mapCode,
                            dataArray: dataArray,
                            payload: payload,
                            cancellationToken: cancellationToken
                        )
                    )
                    .ToList()
            );

            return new APIResponseData<object?>();
        }
        catch (Exception e)
        {
            // Tulis log dan tinggalkan prosesnya (jangan ditunggu)
            _ = new LogDataModel
            {
                BatchId = payload.BatchId,
                ProcessCode = $"[{string.Join(separator: ", ", value: mapCodes)}]",
                ProcessType = LogDataModel.ProcessKind.ML_INSERTDATA,
                ProcessIndex = 1,
                TotalProcess = dataArray.Count,
                Log = e.Message,
                Request = payload.Data.ToString(),
                Response = null,
            }.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);

            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: e,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    private async Task ProcessMapCodeAsync(
        string mapCode,
        JArray dataArray,
        UpsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await ProcessUpsertLogAsync(
                upsertLog: await (
                    await new GroupedMappingModel().Load(
                        systemLogging: systemLogging,
                        pgHelper: pgHelper,
                        caching: caching,
                        mapCode: mapCode,
                        cancellationToken: cancellationToken
                    )
                    ?? throw new DataException(
                        s: $"Map configuration with code {mapCode} not found."
                    )
                ).DoUpsert(
                    payload: payload,
                    pgHelper: pgHelper,
                    cancellationToken: cancellationToken
                ),
                mapCode: mapCode,
                dataArray: dataArray,
                payload: payload,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            await SaveErrorLogAsync(
                mapCode: mapCode,
                payload: payload,
                dataArray: dataArray,
                e: e,
                processIndex: payload.ProcessIndex,
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task ProcessUpsertLogAsync(
        StringBuilder upsertLog,
        string mapCode,
        JArray dataArray,
        UpsertAndAggregatePayloadModel payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var logEntries = new List<(int Index, string Message)>();

            foreach (var logItem in JArray.Parse(json: upsertLog.ToString()))
            {
                var logString = logItem.ToString();
                var colonIndex = logString.IndexOf(value: ": ");
                if (
                    colonIndex <= 0
                    || !int.TryParse(s: logString[..colonIndex], result: out var index)
                )
                    continue;

                var message = logString[(colonIndex + 2)..];
                logEntries.Add(item: (index, message));
            }

            await Task.WhenAll(
                tasks: logEntries.Select(selector: logEntry =>
                    SaveLogEntryAsync(
                        mapCode: mapCode,
                        dataArray: dataArray,
                        payload: payload,
                        index: logEntry.Index,
                        message: logEntry.Message,
                        cancellationToken: cancellationToken
                    )
                )
            );
        }
        catch (Exception ex)
        {
            await SaveErrorLogAsync(
                mapCode: mapCode,
                payload: payload,
                dataArray: dataArray,
                e: new Exception(
                    message: $"Error parsing upsert log: {ex.Message}\nOriginal Log: {upsertLog}",
                    innerException: ex
                ),
                processIndex: payload.ProcessIndex,
                cancellationToken: cancellationToken
            );
        }
    }

    private Task SaveLogEntryAsync(
        string mapCode,
        JArray dataArray,
        UpsertAndAggregatePayloadModel payload,
        int index,
        string message,
        CancellationToken cancellationToken
    )
    {
        _ = new LogDataModel
        {
            BatchId = payload.BatchId,
            ProcessCode = mapCode,
            ProcessType = LogDataModel.ProcessKind.ML_INSERTDATA,
            ProcessIndex = index,
            TotalProcess = dataArray.Count,
            Log = message,
            Request = dataArray[
                // Pastikan index valid
                index: Math.Clamp(value: index - 1, min: 0, max: dataArray.Count - 1)
            ]
                .ToString(),
            Response = null,
        }.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);

        return Task.CompletedTask;
    }

    private Task SaveErrorLogAsync(
        string mapCode,
        UpsertAndAggregatePayloadModel payload,
        JArray dataArray,
        Exception e,
        int processIndex,
        CancellationToken cancellationToken
    )
    {
        _ = new LogDataModel
        {
            BatchId = payload.BatchId,
            ProcessCode = mapCode,
            ProcessType = LogDataModel.ProcessKind.ML_INSERTDATA,
            ProcessIndex = processIndex,
            TotalProcess = dataArray.Count,
            Log = e.Message,
            Request = payload.Data.ToString(),
            Response = null,
        }.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);

        return Task.CompletedTask;
    }
}
