using IDC.AggrMapping.Utilities.Models;
using IDC.AggrMapping.Utilities.Models.Postgre;
using IDC.Utilities;
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

internal static class InsertDataData
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

    // TODO: uncomment code di bawah ini dan update nama sp dan parameternya
    internal static async Task<int> ProcessMapToDB(
        this GroupedMappingModel? groupMapData,
        MlaPayloadModel payloadData,
        // PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(groupMapData);
        return 1;

#pragma warning disable S125
        // await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        // (_, var affectedRows) = await pgHelper.ExecuteNonQueryAsync(
        //     spCallInfo: new PostgreHelper.SPCallInfo
        //     {
        //         Schema = "aggregation",
        //         SPName = "insert_data",
        //         Parameters =
        //         [
        //             new PostgreHelper.SPParameter
        //             {
        //                 Name = "p_group_map_data",
        //                 Value = groupMapData.ToString(),
        //             },
        //             new PostgreHelper.SPParameter
        //             {
        //                 Name = "p_payload_data",
        //                 Value = payloadData.ToString(),
        //             },
        //         ],
        //     },
        //     cancellationToken: cancellationToken
        // );

        // return affectedRows ?? throw new InvalidOperationException("Failed to insert data.");
#pragma warning restore S125
    }
}

internal static class CustomLoggingData
{
    private static string GroupMapMsgBuilder(
        string mapCode,
        string status,
        GroupedMappingModel? groupMapData
    )
    {
        var header =
            @$"
[ Loading Map Configuration ]
MapCode: {mapCode}
Status: {status}";
        if (groupMapData != null)
            header +=
                $@"
Details Configurations:
{groupMapData.ToJsonString()}
";

        return header;
    }

    private static async Task LogWriter(
        string msg,
        SystemLogging systemLogging,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Run(
            action: () =>
            {
                systemLogging.LogInformation(msg);
            },
            cancellationToken: cancellationToken
        );
    }

    internal static async Task WriteSuccessLog(
        this GroupedMappingModel groupMapData,
        string mapCode,
        SystemLogging systemLogging,
        CancellationToken cancellationToken = default
    ) =>
        await LogWriter(
            msg: GroupMapMsgBuilder(
                mapCode: mapCode,
                status: "Success",
                groupMapData: groupMapData
            ),
            systemLogging: systemLogging,
            cancellationToken: cancellationToken
        );

    internal static async Task WriteFailLog(
        this GroupedMappingModel? groupMapData,
        string mapCode,
        SystemLogging systemLogging,
        CancellationToken cancellationToken = default
    ) =>
        await LogWriter(
            msg: GroupMapMsgBuilder(mapCode: mapCode, status: "Failed", groupMapData: null),
            systemLogging: systemLogging,
            cancellationToken: cancellationToken
        );
}
