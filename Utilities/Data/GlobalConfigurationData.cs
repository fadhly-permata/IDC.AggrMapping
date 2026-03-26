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
            callback: _ => { },
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
        mapCode.ThrowIfNullOrWhitespace(paramName: nameof(mapCode));

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
            callback: _ => { },
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
        aggregateCode.ThrowIfNullOrWhitespace(paramName: nameof(aggregateCode));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_aggr_config_by_code",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_gc_code", Value = aggregateCode },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return new AggregateConfigurationModel
        {
            Code = aggregateCode,
            Configurations = JObject.Parse(json: data as string ?? "{}"),
        };
    }
}

internal static class AggregateDataFe
{
    internal static async Task<JArray?> FE_GetDataGrid(
        this PostgreHelper pgHelper,
        string? gridType = "ALL",
        CancellationToken cancellationToken = default
    )
    {
        gridType.ThrowIfNullOrWhitespace(paramName: nameof(gridType));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_get_aggregation_grid",
                Parameters = [new PostgreHelper.SPParameter { Name = "p_mode", Value = gridType }],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JArray.Parse(json: data as string ?? "[]");
    }

    internal static async Task<JObject?> FE_GetDetail(
        this PostgreHelper pgHelper,
        int id,
        CancellationToken cancellationToken = default
    )
    {
        if (id < 1)
            throw new ArgumentException(
                paramName: nameof(id),
                message: "Id must be greater than 0."
            );

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_get_aggregation_detail",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_id",
                        Value = id,
                        DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
    }

    internal static async Task<JObject?> FE_Remove(
        this PostgreHelper pgHelper,
        int id,
        string? username,
        CancellationToken cancellationToken = default
    )
    {
        if (id < 1)
            throw new ArgumentException(
                paramName: nameof(id),
                message: "Id must be greater than 0."
            );

        username.ThrowIfNullOrWhitespace(
            paramName: nameof(username),
            message: "Username cannot be null, empty or whitespace."
        );

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_remove_aggregation_data",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_id",
                        Value = id,
                        DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    },
                    new PostgreHelper.SPParameter { Name = "p_username", Value = username },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
    }

    internal static async Task<JObject?> FE_Rollback(
        this PostgreHelper pgHelper,
        int id,
        string? username,
        CancellationToken cancellationToken = default
    )
    {
        if (id < 1)
            throw new ArgumentException(
                paramName: nameof(id),
                message: "Id must be greater than 0."
            );

        username.ThrowIfNullOrWhitespace(
            paramName: nameof(username),
            message: "Username cannot be null, empty or whitespace."
        );

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_rollback_aggregation",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_id",
                        Value = id,
                        DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    },
                    new PostgreHelper.SPParameter { Name = "p_user", Value = username },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
    }

    internal static async Task<JObject?> FE_Log(
        this PostgreHelper pgHelper,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        code.ThrowIfNullOrWhitespace(
            paramName: nameof(code),
            message: "Code cannot be null, empty or whitespace."
        );

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_log_aggregation",
                Parameters = [new PostgreHelper.SPParameter { Name = "p_code", Value = code }],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
    }

    internal static async Task<JObject?> FE_Copy(
        this PostgreHelper pgHelper,
        int id,
        string? name,
        string? username,
        CancellationToken cancellationToken = default
    )
    {
        if (id < 1)
            throw new ArgumentException(
                paramName: nameof(id),
                message: "Id must be greater than 0."
            );

        name.ThrowIfNullOrWhitespace(
            paramName: nameof(name),
            message: "Name cannot be null, empty or whitespace."
        );

        username.ThrowIfNullOrWhitespace(
            paramName: nameof(username),
            message: "Username cannot be null, empty or whitespace."
        );

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_copy_aggregation",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_id",
                        Value = id,
                        DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    },
                    new PostgreHelper.SPParameter { Name = "p_name", Value = name },
                    new PostgreHelper.SPParameter { Name = "p_user", Value = username },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
    }

    internal static async Task<JObject?> FE_Approval(
        this PostgreHelper pgHelper,
        int id,
        string? username,
        string? role,
        string? action,
        string? note,
        CancellationToken cancellationToken = default
    )
    {
        if (id < 1)
            throw new ArgumentException(
                paramName: nameof(id),
                message: "Id must be greater than 0."
            );

        username.ThrowIfNullOrWhitespace(
            paramName: nameof(username),
            message: "Username cannot be null, empty or whitespace."
        );

        role.ThrowIfNullOrWhitespace(
            paramName: nameof(role),
            message: "Role cannot be null, empty or whitespace."
        );

        action.ThrowIfNullOrWhitespace(
            paramName: nameof(action),
            message: "Action cannot be null, empty or whitespace."
        );

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_approval_proc_aggregation",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_id",
                        Value = id,
                        DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                    },
                    new PostgreHelper.SPParameter { Name = "p_user", Value = username },
                    new PostgreHelper.SPParameter { Name = "p_role", Value = role },
                    new PostgreHelper.SPParameter { Name = "p_action", Value = action },
                    new PostgreHelper.SPParameter { Name = "p_note", Value = note },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
    }

    public record AggrConfigUpsertModel(
        string? Code,
        string? Name,
        string? Desc,
        string? DataApplied,
        string? Type,
        string? JsonList,
        string? jsonCondition,
        string? configFinal
    );

    internal static async Task<JObject?> FE_Upsert(
        this PostgreHelper pgHelper,
        string? username,
        int? id,
        string? code,
        string? name,
        string? desc,
        string? dataApplied,
        string? type,
        string? jsonList,
        string? jsonCondition,
        string? configFinal,
        CancellationToken cancellationToken = default
    )
    {
        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, data) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_upsert_aggregation",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_user", Value = username },
                    new PostgreHelper.SPParameter { Name = "p_id", Value = id },
                    new PostgreHelper.SPParameter { Name = "p_code", Value = code },
                    new PostgreHelper.SPParameter { Name = "p_name", Value = name },
                    new PostgreHelper.SPParameter { Name = "p_desc", Value = desc },
                    new PostgreHelper.SPParameter { Name = "p_data_applied", Value = dataApplied },
                    new PostgreHelper.SPParameter { Name = "p_type", Value = type },
                    new PostgreHelper.SPParameter { Name = "p_json_list", Value = jsonList },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_json_condition",
                        Value = jsonCondition,
                    },
                    new PostgreHelper.SPParameter { Name = "p_config_final", Value = configFinal },
                ],
            },
            callback: _ => { },
            cancellationToken: cancellationToken
        );

        return JObject.Parse(json: data as string ?? "{}");
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
