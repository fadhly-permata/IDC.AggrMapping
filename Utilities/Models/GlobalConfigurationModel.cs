using System.Text.Json.Serialization;
using IDC.AggrMapping.Utilities.Data;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Represents global configurations for the application.
/// </summary>
public partial class GlobalConfigurationModel : BaseModel<GlobalConfigurationModel>
{
    /// <summary>
    ///     Gets or sets the maximum number of parallel processes.
    /// </summary>
    [JsonPropertyName("maxParallelProcess")]
    public int MaxParallelProcess { get; set; } = DFLT_MAX_PRL_PROC;

    /// <summary>
    ///     Gets or sets the maximum depth for processing.
    /// </summary>
    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; } = DFLT_MAX_DEPTH;

    /// <summary>
    ///     Gets or sets the maximum number of map items allowed.
    /// </summary>
    [JsonPropertyName("maxMapCount")]
    public int MaxMapCount { get; set; } = DFLT_MAX_MAP_CNT;

    /// <summary>
    ///     Gets or sets the maximum data payload size.
    /// </summary>
    [JsonPropertyName("maxDataPayload")]
    public int MaxDataPayload { get; set; } = DFLT_MAX_DATA_PLOAD;

    /// <summary>
    ///     Gets or sets the maximum number of map items allowed in a group.
    /// </summary>
    [JsonPropertyName("maxGroupMapCount")]
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

    internal async Task<GlobalConfigurationModel> InitFromDatabase(
        PostgreHelper pgHelper,
        Caching caching,
        CancellationToken cancellationToken = default
    ) =>
        await caching.GetOrSetAsync(
            key: "GlobalConfigurations",
            valueFactory: async () =>
            {
                var cData = await pgHelper.GetGlobalConfigurationsAsync(
                    configCodes: ["GAM01", "GAM02", "GAM03", "GAM04", "GAM05"],
                    cancellationToken: cancellationToken
                );

                if (cData is null)
                    return this;

                MaxParallelProcess = cData.PropGet("GAM01", defaultValue: DFLT_MAX_PRL_PROC);
                MaxDepth = cData.PropGet("GAM02", defaultValue: DFLT_MAX_DEPTH);
                MaxMapCount = cData.PropGet("GAM03", defaultValue: DFLT_MAX_MAP_CNT);
                MaxDataPayload = cData.PropGet("GAM04", defaultValue: DFLT_MAX_DATA_PLOAD);
                MaxGroupMapCount = cData.PropGet("GAM05", defaultValue: DFLT_MAX_GRP_MAP_CNT);

                return this;
            },
            expirationRenewal: true
        );
}
