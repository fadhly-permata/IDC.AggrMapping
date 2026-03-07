using System.Text.Json.Serialization;
using IDC.AggrMapping.Utilities.Data;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Represents global configurations for the application.
/// </summary>
public class GlobalConfigurationModel : BaseModel<GlobalConfigurationModel>
{
    private const int DFLT_MAX_PRL_PROC = 20;
    private const int DFLT_MAX_DEPTH = 3;
    private const int DFLT_MAX_MAP_CNT = 5;
    private const int DFLT_MAX_DATA_PLOAD = 20;

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

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"MaxParallelProcess: {MaxParallelProcess}, MaxDepth: {MaxDepth}, MaxMapCount: {MaxMapCount}, MaxDataPayload: {MaxDataPayload}";
    }

    /// <summary>
    ///     Initializes the global configurations from the database.
    /// </summary>
    /// <param name="pgHelper">
    ///     The PostgreHelper instance.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token.
    /// </param>
    /// <returns>
    ///     The initialized GlobalConfigurations instance.
    /// </returns>
    public async Task<GlobalConfigurationModel> InitFromDatabase(
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        var configData = await GlobalConfigurationData.GetGlobalConfigurationsAsync(
            pgHelper: pgHelper,
            configCodes: ["GAM01", "GAM02", "GAM03", "GAM04"],
            cancellationToken: cancellationToken
        );

        if (configData is null)
            return this;

        MaxParallelProcess = configData.PropGet("GAM01", defaultValue: DFLT_MAX_PRL_PROC);
        MaxDepth = configData.PropGet("GAM02", defaultValue: DFLT_MAX_DEPTH);
        MaxMapCount = configData.PropGet("GAM03", defaultValue: DFLT_MAX_MAP_CNT);
        MaxDataPayload = configData.PropGet("GAM04", defaultValue: DFLT_MAX_DATA_PLOAD);

        return this;
    }
}
