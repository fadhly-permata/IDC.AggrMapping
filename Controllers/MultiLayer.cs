using System.Data;
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
///     asdasd
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
    ///     asdasd
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="DataException"></exception>
    [Tags(tags: "Multi Layer"), HttpPost(template: "MultiLayer")]
    public async Task<APIResponseData<object?>> Process(
        MultiLayerPayloadModel payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            payload.Validate(
                configs: await new GlobalConfigurationModel().GetGlobalConfigurationsAsync(
                    pgHelper: pgHelper,
                    caching: caching,
                    cancellationToken: cancellationToken
                )
            );

            var batchId = await BatchIdGenerator(cancellationToken: cancellationToken);

            await UpsertProcess(
                batchId: batchId,
                payload: payload,
                cancellationToken: cancellationToken
            );

            for (var index = 0; index < payload.Data.Count; index++)
            {
                try
                {
                    var aggPload = payload.CastToAggregatePayload(
                        batchId: batchId,
                        totalProcess: payload.Data.Count,
                        dataIndex: index
                    );

                    var result = await new AggregateEngine(
                        systemLogging: systemLogging,
                        caching: caching,
                        pgHelper: pgHelper
                    ).SingleAggregate(payload: aggPload, cancellationToken: cancellationToken);

                    if (result.Status != null || result.Status?.ToLower() == "failed")
                        throw new DataException(
                            s: "Aggregation process failed, can not continue to processing workflow."
                        );

                    // objek buat WF/DF
                    var joResult =
                        result.Data as JObject
                        ?? throw new DataException(
                            s: $"Failed to get result of aggregation with code '{payload.ConfAggMapCode}'."
                        );

                    object dfWfResult;
                    if (payload.FlowCode[..1] == "W")
                    {
                        // PROSES HIT WF
                        var token = await GenerateToken(cancellationToken: cancellationToken);
                        var wfData = payload.Data[index: index] as JObject;

                        dfWfResult = await WorkFlowProcessor(
                            body: wfData!,
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

                        // PROCESS HIT DF
                        dfWfResult = await DecisionFlowProcessor(
                            body: new JObject()
                            {
                                { "code", payload.FlowCode },
                                { "data", dfData },
                            },
                            cancellationToken: cancellationToken
                        );
                    }

                    Console.WriteLine(value: dfWfResult);
                }
                catch (Exception e)
                {
                    Console.WriteLine(value: e.Message);
                }
            }

            return new APIResponseData<object?>().ChangeData(data: batchId);
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
            options.AppSettings.Get<string>(options.BaseAddressKey)
            ?? throw new InvalidOperationException(
                $"Can not find API Settings for '{options.BaseAddressKey}'."
            );

        var apiResult =
            await options.HttpClient.PostJObjectAsync(
                uri: $"{baseAddress}{options.Endpoint}",
                content: options.Content,
                headers: options.Headers,
                cancellationToken: options.CancellationToken
            ) ?? throw new InvalidOperationException(options.ErrorMessage);

        return !options.ValidateResponse(apiResult)
            ? throw new InvalidOperationException(options.ErrorMessage)
            : options.ExtractData(apiResult);
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
                    new ExecuteApiRequestOptions<string>
                    {
                        HttpClient = httpClient,
                        BaseAddressKey = "APISettings.urlAPI_idccore",
                        Endpoint = "/token",
                        Content = null,
                        Headers = new Dictionary<string, string>()
                        {
                            {
                                "username",
                                appSettings.Get<string>("TokenAuthentication.UserName")
                                    ?? throw new InvalidOperationException(
                                        "Can not find username for token generation."
                                    )
                            },
                            {
                                "password",
                                appSettings.Get<string>("TokenAuthentication.Password")
                                    ?? throw new InvalidOperationException(
                                        "Can not find password for token generation."
                                    )
                            },
                        },
                        ValidateResponse = response =>
                            response.PropGet<bool?>("success", throwOnNull: true) == true,
                        ExtractData = response =>
                            response.PropGet<string>("access_token", throwOnNull: true)
                            ?? throw new InvalidOperationException(
                                "Access token not found in response."
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
                    response.PropGet<JObject>("data", throwOnNull: true)
                    ?? throw new InvalidOperationException("Data not found in response."),
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
                    response.PropGet<JObject>("data", throwOnNull: true)
                    ?? throw new InvalidOperationException("Data not found in response."),
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
