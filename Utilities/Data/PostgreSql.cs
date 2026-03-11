using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Data;

internal class PostgreSql(PostgreHelper pgHelper)
{
    internal async Task<JArray?> GetAllDataVersion(
        CancellationToken cancellationToken = default
    )
    {
        object? result;

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);

        (pgHelper, result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_all_version_aggr_config",
                Parameters = [],
            },
            cancellationToken: cancellationToken
        );

        string resultString = result?.CastToString() ?? "[]";
        return JArray.Parse(string.IsNullOrWhiteSpace(resultString) ? "[]" : resultString);
    }

    internal async Task<JArray?> GetAllDataActive(
        CancellationToken cancellationToken = default
    )
    {
        object? result;

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);

        (pgHelper, result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_all_active_aggr_config",
                Parameters = [],
            },
            cancellationToken: cancellationToken
        );

        string resultString = result?.CastToString() ?? "[]";
        return JArray.Parse(string.IsNullOrWhiteSpace(resultString) ? "[]" : resultString);
    }
}
