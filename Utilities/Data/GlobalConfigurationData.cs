using IDC.Utilities.Data;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Data;

internal static class GlobalConfigurationData
{
    internal static async Task<JObject?> GetGlobalConfigurationsAsync(
        PostgreHelper pgHelper,
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
