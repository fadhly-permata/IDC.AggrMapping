using System.Text.Json.Serialization;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Represents global configurations for the application.
/// </summary>
public partial class GlobalConfigurationModel : BaseModel<GlobalConfigurationModel>
{
    /// <summary>
    ///     Gets or sets the maximum number of parallel processes.
    /// </summary>
    [JsonPropertyName(name: "maxParallelProcess")]
    public int MaxParallelProcess { get; set; } = DFLT_MAX_PRL_PROC;

    /// <summary>
    ///     Gets or sets the maximum depth for processing.
    /// </summary>
    [JsonPropertyName(name: "maxDepth")]
    public int MaxDepth { get; set; } = DFLT_MAX_DEPTH;

    /// <summary>
    ///     Gets or sets the maximum number of map items allowed.
    /// </summary>
    [JsonPropertyName(name: "maxMapCount")]
    public int MaxMapCount { get; set; } = DFLT_MAX_MAP_CNT;

    /// <summary>
    ///     Gets or sets the maximum data payload size.
    /// </summary>
    [JsonPropertyName(name: "maxDataPayload")]
    public int MaxDataPayload { get; set; } = DFLT_MAX_DATA_PLOAD;

    /// <summary>
    ///     Gets or sets the maximum number of map items allowed in a group.
    /// </summary>
    [JsonPropertyName(name: "maxGroupMapCount")]
    public int MaxGroupMapCount { get; set; } = DFLT_MAX_GRP_MAP_CNT;
}

public partial class GlobalConfigurationModel
{
    private const int DFLT_MAX_PRL_PROC = 20;
    private const int DFLT_MAX_DEPTH = 3;
    private const int DFLT_MAX_MAP_CNT = 5;
    private const int DFLT_MAX_DATA_PLOAD = 20;
    private const int DFLT_MAX_GRP_MAP_CNT = 5;

    /// <inheritdoc/>
    public override string ToString() =>
        $"MaxParallelProcess: {MaxParallelProcess}, MaxDepth: {MaxDepth}, MaxMapCount: {MaxMapCount}, MaxDataPayload: {MaxDataPayload}";

    internal async Task<GlobalConfigurationModel> GetGlobalConfigurationsAsync(
        PostgreHelper pgHelper,
        Caching caching,
        CancellationToken cancellationToken = default
    ) =>
        await caching.GetOrSetAsync(
            key: "GlobalConfigurations",
            valueFactory: async () =>
            {
                var cData = await InitFromDatabaseAsync(
                    pgHelper: pgHelper,
                    configCodes: ["GAM01", "GAM02", "GAM03", "GAM04", "GAM05"],
                    cancellationToken: cancellationToken
                );

                if (cData is null)
                    return this;

                MaxParallelProcess = cData.PropGet(path: "GAM01", defaultValue: DFLT_MAX_PRL_PROC);
                MaxDepth = cData.PropGet(path: "GAM02", defaultValue: DFLT_MAX_DEPTH);
                MaxMapCount = cData.PropGet(path: "GAM03", defaultValue: DFLT_MAX_MAP_CNT);
                MaxDataPayload = cData.PropGet(path: "GAM04", defaultValue: DFLT_MAX_DATA_PLOAD);
                MaxGroupMapCount = cData.PropGet(path: "GAM05", defaultValue: DFLT_MAX_GRP_MAP_CNT);

                return this;
            },
            expirationRenewal: true
        );

    internal static async Task<JObject?> InitFromDatabaseAsync(
        PostgreHelper pgHelper,
        string[] configCodes,
        CancellationToken cancellationToken = default
    )
    {
        if (configCodes == null || configCodes.Length == 0)
            throw new ArgumentNullException(paramName: nameof(configCodes));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        var (_, result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "acv_get_global_configs",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_config_codes",
                        Value = string.Join(separator: ",", value: configCodes),
                    },
                ],
            },
            callback: static _ => { },
            cancellationToken: cancellationToken
        );

        return result != null ? JObject.Parse(json: result.ToString() ?? "{}") : null;
    }
}
