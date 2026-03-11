using IDC.AggrMapping.Utilities.Models.Postgre;
using IDC.Utilities;

namespace IDC.AggrMapping.Utilities.Data;

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

    private static string WriteUpsertMsgBuilder(
        string mapCode,
        string status,
        string generatedQuery
    )
    {
        return @$"
[ Upsert Map Configuration ]
MapCode: {mapCode}
Status: {status}
Generated Query: 
{generatedQuery}
";
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

    internal static async Task WriteUpsertLog(
        string mapCode,
        string status,
        string generatedQuery,
        SystemLogging systemLogging,
        CancellationToken cancellationToken = default
    ) =>
        await LogWriter(
            msg: WriteUpsertMsgBuilder(
                mapCode: mapCode,
                status: status,
                generatedQuery: generatedQuery
            ),
            systemLogging: systemLogging,
            cancellationToken: cancellationToken
        );
}
