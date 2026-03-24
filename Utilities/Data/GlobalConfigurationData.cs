using System.ComponentModel;
using Hangfire.Common;
using IDC.AggrMapping.Utilities.Models.AggregateEngine;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Data;

internal static class GlobalConfigurationData
{
    internal static async Task<JObject?> GetGlobalConfigurationsAsync(
        this PostgreHelper pgHelper,
        string[] configCodes,
        CancellationToken cancellationToken = default
    )
    {
        if (configCodes == null || configCodes.Length == 0)
            throw new ArgumentNullException(paramName: nameof(configCodes));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        (_, var result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_global_configs",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_config_codes",
                        Value = string.Join(separator: ",", value: configCodes),
                    },
                ],
            },
            callback: data => { },
            cancellationToken: cancellationToken
        );

        return result != null ? JObject.Parse(json: result.ToString() ?? "{}") : null;
    }
}

internal static class InsertEngineData
{
    internal static async Task<JObject?> GetGroupedMappingAsync(
        this PostgreHelper pgHelper,
        string mapCode,
        CancellationToken cancellationToken = default
    )
    {
        mapCode.ThrowIfNullOrWhitespace(nameof(mapCode));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        (_, var result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_map_configs",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_map_code", Value = mapCode },
                ],
            },
            callback: data => { },
            cancellationToken: cancellationToken
        );

        return result != null ? JObject.Parse(json: result.ToString() ?? "{}") : null;
    }
}

internal static class AggregateData
{
    internal static async Task<AggregateConfigurationModel?> GetAggregateConfigurationAsync(
        this PostgreHelper pgHelper,
        string aggregateCode,
        CancellationToken cancellationToken = default
    )
    {
        aggregateCode.ThrowIfNullOrWhitespace(nameof(aggregateCode));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        (_, var data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_aggr_config_by_code",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_gc_code", Value = aggregateCode },
                ],
            },
            callback: data => { },
            cancellationToken: cancellationToken
        );

        return new AggregateConfigurationModel
        {
            Code = aggregateCode,
            Configurations = JObject.Parse(data as string ?? "{}"),
        };
    }
}

internal static class LoggingData
{
    internal enum ProcessType
    {
        ML_AGGREGATE,
        ML_INSERTDATA,
        ML_WF_OR_DF,
    }

    // p_batch_no character varying, p_aggr_code character varying, p_request json DEFAULT NULL::json, p_response json DEFAULT NULL::json, p_log text DEFAULT NULL::text
    internal record DbLoggingModel(
        string BatchCode,
        ProcessType ProcessType,
        string ProcessCode,
        string Request,
        string Response,
        string Log
    );

    internal static async Task InsertAggregateProLogAsync(
        this PostgreHelper pgHelper,
        DbLoggingModel model,
        CancellationToken cancellationToken = default
    )
    {
        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        await pgHelper.ExecuteNonQueryAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "log_data_proc",
                SPName = "upsert_multilayer_aggregate_proc",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_batch_no", Value = model.BatchCode },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_process_type",
                        Value = model.ProcessType.ToString(),
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_process_code",
                        Value = model.ProcessCode,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_request",
                        Value = model.Request,
                        DataType = NpgsqlTypes.NpgsqlDbType.Json,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_response",
                        Value = model.Response,
                        DataType = NpgsqlTypes.NpgsqlDbType.Json,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_log",
                        Value = model.Log,
                        DataType = NpgsqlTypes.NpgsqlDbType.Text,
                    },
                ],
            },
            cancellationToken: cancellationToken
        );
    }
}
