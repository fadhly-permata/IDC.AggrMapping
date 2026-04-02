using System.Data;
using Hangfire;
using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Comm.Http;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.API;
using IDC.Utilities.Template.ConfigAndSettings;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Controller for Multi-Layer Aggregation
/// </summary>
[Route(template: "AggrMapping/[controller]")]
[ApiController]
public partial class MultiLayer(
    SystemLogging systemLogging,
    Caching caching,
    PostgreHelper pgHelper,
    AppSettings appSettings,
    HttpClientUtility httpClient
) : ControllerBase
{
    /// <summary>
    ///     Processes a multi-layer aggregation and mapping workflow.
    /// </summary>
    /// <remarks>
    ///     This endpoint processes data through multiple layers including:
    ///     - Data aggregation using specified mapping configurations
    ///     - Workflow or decision flow processing based on the flow code
    ///     - Batch processing with detailed logging and error handling
    ///
    ///     Sample request:
    ///     ```json
    ///     POST /AggrMapping/MultiLayer/MultiLayer
    ///     Content-Type: application/json
    ///
    ///     {
    ///         "flow_code": "D12345",
    ///         "wftype": "SampleWorkflow",
    ///         "confMaptable": ["MAP001", "MAP002"],
    ///         "confAggMapCode": "AGG001",
    ///         "data": [
    ///             { "field1": "value1" },
    ///             { "field1": "value2" }
    ///         ]
    ///     }
    ///     ```
    ///
    ///     **Notes:**
    ///     - `flow_code` determines the type of flow to execute (Workflow or Decision Flow)
    ///     - When `flow_code` starts with 'W', `wftype` is required
    ///     - `confMaptable` must contain at least one valid mapping configuration
    ///     - `confAggMapCode` specifies the aggregation configuration to use
    ///     - `data` contains the payload to be processed (max items configured in global settings)
    /// </remarks>
    /// <param name="payload">
    ///     The multi-layer processing payload containing:
    ///     - flow_code (string, required): Determines the processing flow (starts with 'W' for workflow, 'D' for decision flow)
    ///     - wftype (string, optional): Required when flow_code starts with 'W'
    ///     - confMaptable (array of strings, required): List of mapping configuration codes
    ///     - confAggMapCode (string, required): Aggregation mapping configuration code
    ///     - data (array of objects, required): The data to be processed
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the asynchronous operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Error message if status is "Failed"
    ///     - Data: Batch ID of the processed request
    /// </returns>
    /// <exception cref="DataException">
    ///     Thrown when:
    ///     - Validation of payload fails
    ///     - Aggregation process fails
    ///     - Workflow/Decision flow processing fails
    /// </exception>
    [Tags(tags: "Multi Layer"), HttpPost(template: "MultiLayer")]
    public async Task<APIResponseData<object?>> ProcessAsync(
        MultiLayerPayloadModel payload,
        CancellationToken cancellationToken
    )
    {
        var batchId = string.Empty;

        try
        {
            batchId = await BatchIdGenerator(cancellationToken: cancellationToken);

            var gcm = await new GlobalConfigurationModel().GetGlobalConfigurationsAsync(
                pgHelper: pgHelper,
                caching: caching,
                cancellationToken: cancellationToken
            );
            systemLogging.LogInformation(message: $"[GLOBAL CONFIG DATA] \n{gcm}");

            payload.Validate(configs: gcm);

            await UpsertProcess(
                batchId: batchId,
                payload: payload,
                cancellationToken: cancellationToken
            );

            // proses setiap data dengan menggunakan job.
            for (var index = 0; index < payload.Data.Count; index++)
            {
                var cIndex = index;
                await ExecuteAggregationAndFlowAsync(
                    payload: payload,
                    batchId: batchId,
                    index: cIndex,
                    cancellationToken: cancellationToken
                );

                // BackgroundJob.Schedule(
                //     methodCall: () =>
                //         ExecuteAggregationAndFlowAsync(payload, batchId, cIndex, cancellationToken),
                //     delay: TimeSpan.FromSeconds(1)
                // );
            }

            return new APIResponseData<object?>();
        }
        catch (Exception ex)
        {
            _ = new LogDataModel
            {
                BatchId = batchId,
                ProcessCode = payload.ConfAggMapCode,
                ProcessType = LogDataModel.ProcessKind.ML_WF_OR_DF,
                ProcessIndex = 0,
                TotalProcess = payload.Data.Count,
                Log = ex.Message,
                Request = payload.Data.ToString(),
                Response = null,
            }.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);

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
    ///     Proses Aggregation dan WF/DF
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true), HttpPost(template: "AggregationAndFlow")]
    public async Task ExecuteAggregationAndFlowAsync(
        MultiLayerPayloadModel payload,
        string batchId,
        int index,
        CancellationToken cancellationToken
    )
    {
        JObject? bodyRequest = [];
        var aggResult = await new AggregateEngine(
            systemLogging: systemLogging,
            caching: caching,
            pgHelper: pgHelper
        ).SingleAggregate(
            payload: payload.CastToAggregatePayload(
                batchId: batchId,
                totalProcess: payload.Data.Count,
                dataIndex: index
            ),
            cancellationToken: cancellationToken
        );

        // jalankan WF/DF
        try
        {
            if (aggResult == null || aggResult.Status?.ToLower() == "failed")
                throw new DataException(
                    s: "Aggregation process failed, can not continue to processing workflow."
                );

            // data objek buat WF/DF
            var joResult =
                aggResult.Data as JObject
                ?? throw new DataException(
                    s: $"Failed to get result of aggregation with code '{payload.ConfAggMapCode}'."
                );

            object dfWfResult;
            if (payload.FlowCode[..1] == "W")
            {
                // PROSES HIT WF
                var token = await GenerateToken(cancellationToken: cancellationToken);
                bodyRequest = payload.Data[index: index] as JObject;

                dfWfResult = await WorkFlowProcessor(
                    body: bodyRequest!,
                    headers: new Dictionary<string, string>()
                    {
                        { "wfcode", payload.FlowCode },
                        {
                            "wftype",
                            payload.WorkflowType
                                ?? throw new DataException(
                                    s: "Can not hit workflow without wftype."
                                )
                        },
                        { "Authorization", $"Bearer {token}" },
                    },
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                var dfData = payload.Data[index: index] as JObject;
                dfData?.Merge(content: joResult);

                bodyRequest = new JObject()
                {
                    { "code", payload.FlowCode },
                    { "data", new JArray(dfData) },
                };

                // PROCESS HIT DF
                dfWfResult = await DecisionFlowProcessor(
                    body: bodyRequest,
                    cancellationToken: cancellationToken
                );
            }

            _ = new LogDataModel
            {
                BatchId = batchId,
                ProcessCode = payload.FlowCode,
                ProcessType = LogDataModel.ProcessKind.ML_WF_OR_DF,
                ProcessIndex = index + 1,
                TotalProcess = payload.Data.Count,
                Log = null,
                Request = bodyRequest?.ToString(),
                Response = dfWfResult.ToString(),
            }.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _ = new LogDataModel
            {
                BatchId = batchId,
                ProcessCode = payload.FlowCode,
                ProcessType = LogDataModel.ProcessKind.ML_WF_OR_DF,
                ProcessIndex = index + 1,
                TotalProcess = payload.Data.Count,
                Log = e.Message,
                Request = bodyRequest?.ToString() ?? payload.Data[index: index].ToString(),
                Response = null,
            }.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);
        }
    }
}

public partial class MultiLayer
{
    private sealed record ExecuteApiRequestOptions<T>
    {
        public required HttpClientUtility HttpClient { get; init; }
        public required string BaseAddressKey { get; init; }
        public required string Endpoint { get; init; }
        public object? Content { get; init; }
        public Dictionary<string, string> Headers { get; init; } = [];
        public required Func<JObject, bool> ValidateResponse { get; init; }
        public required Func<JObject, T> ExtractData { get; init; }
        public required AppSettings AppSettings { get; init; }
        public required string ErrorMessage { get; init; }
        public CancellationToken CancellationToken { get; init; }
    }

    private static async Task<T> ExecuteApiRequestAsync<T>(ExecuteApiRequestOptions<T> options)
    {
        var baseAddress =
            options.AppSettings.Get<string>(path: options.BaseAddressKey)
            ?? throw new InvalidOperationException(
                message: $"Can not find API Settings for '{options.BaseAddressKey}'."
            );

        var apiResult = await options.HttpClient.PostJObjectAsync(
            uri: $"{baseAddress}{options.Endpoint}",
            content: options.Content,
            headers: options.Headers,
            timeoutSeconds: 120,
            ensureStatusCodeSuccess: false,
            cancellationToken: options.CancellationToken
        ); // ?? throw new InvalidOperationException(message: options.ErrorMessage);

        return !options.ValidateResponse(arg: apiResult)
            ? throw new InvalidOperationException(message: apiResult.ToString())
            : options.ExtractData(arg: apiResult);
    }

    private async Task UpsertProcess(
        string batchId,
        MultiLayerPayloadModel payload,
        CancellationToken cancellationToken = default
    )
    {
        if (payload.ConfMaptable.Count > 0)
            await new UpsertEngine(
                caching: caching,
                pgHelper: pgHelper,
                systemLogging: systemLogging
            ).Upsert(
                payload: payload.CastToUpsertPayload(
                    batchId: batchId,
                    mapCode: string.Join(separator: ",", values: payload.ConfMaptable)
                ),
                cancellationToken: cancellationToken
            );
    }

    private async Task<string> GenerateToken(CancellationToken cancellationToken = default)
    {
        return await caching.GetOrSetAsync(
            key: "api_token",
            valueFactory: async () =>
            {
                var token = await ExecuteApiRequestAsync(
                    options: new ExecuteApiRequestOptions<string>
                    {
                        HttpClient = httpClient,
                        BaseAddressKey = "APISettings.urlAPI_idccore",
                        Endpoint = "/token",
                        Content = null,
                        Headers = new Dictionary<string, string>()
                        {
                            {
                                "username",
                                appSettings.Get<string>(path: "TokenAuthentication.UserName")
                                    ?? throw new InvalidOperationException(
                                        message: "Can not find username for token generation."
                                    )
                            },
                            {
                                "password",
                                appSettings.Get<string>(path: "TokenAuthentication.Password")
                                    ?? throw new InvalidOperationException(
                                        message: "Can not find password for token generation."
                                    )
                            },
                        },
                        ValidateResponse = response =>
                            response.PropGet<bool?>(path: "success", throwOnNull: true) == true,
                        ExtractData = response =>
                            response.PropGet<string>(path: "access_token", throwOnNull: true)
                            ?? throw new InvalidOperationException(
                                message: "Access token not found in response."
                            ),
                        AppSettings = appSettings,
                        ErrorMessage = "Fetching API token failed.",
                        CancellationToken = cancellationToken,
                    }
                );

                return token;
            },
            expirationRenewal: false,
            expirationMinutes: 1800
        );
    }

    private async Task<JObject> DecisionFlowProcessor(
        JObject body,
        CancellationToken cancellationToken = default
    )
    {
        return await ExecuteApiRequestAsync(
            options: new ExecuteApiRequestOptions<JObject>
            {
                HttpClient = httpClient,
                BaseAddressKey = "APISettings.urlAPI_idcdecision",
                Endpoint = "/Process/Start",
                Content = body,
                Headers = [],
                ValidateResponse = response =>
                    response.PropGet<string>(path: "status", throwOnNull: true) != "Failed",
                ExtractData = response =>
                    response.PropGet<JObject>(path: "data", throwOnNull: true)
                    ?? throw new InvalidOperationException(message: "Data not found in response."),
                AppSettings = appSettings,
                ErrorMessage = "Calling decision flow failed.",
                CancellationToken = cancellationToken,
            }
        );
    }

    private async Task<JObject> WorkFlowProcessor(
        JObject body,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default
    )
    {
        return await ExecuteApiRequestAsync(
            options: new ExecuteApiRequestOptions<JObject>
            {
                HttpClient = httpClient,
                BaseAddressKey = "APISettings.urlAPI_idcwfstart_submit",
                Endpoint = string.Empty,
                Content = body,
                Headers = headers,
                ValidateResponse = response =>
                    response.PropGet<string>(path: "status", throwOnNull: true) != "Failed",
                ExtractData = response =>
                    response.PropGet<JObject>(path: "data", throwOnNull: true)
                    ?? throw new InvalidOperationException(message: "Data not found in response."),
                AppSettings = appSettings,
                ErrorMessage = "Calling work flow failed.",
                CancellationToken = cancellationToken,
            }
        );
    }

    private async Task<string> BatchIdGenerator(CancellationToken cancellationToken = default)
    {
        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, resultBatchNo) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "log_data_proc",
                SPName = "acv_generate_batch_no",
            },
            callback: static _ => { },
            cancellationToken: cancellationToken
        );

        var strBatchNo =
            resultBatchNo as string ?? throw new DataException(s: "Failed to get batch no.");
        return strBatchNo;
    }
}
