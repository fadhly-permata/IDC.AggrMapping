using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

internal partial class AggConfigEngineModel : BaseModel<AggConfigEngineModel>
{
    [
        JsonProperty(propertyName: "aggregate_code"),
        JsonPropertyName(name: "aggregate_code"),
        Required(AllowEmptyStrings = false)
    ]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the data.
    /// </summary>
    [
        JsonProperty(propertyName: "configuration_items"),
        JsonPropertyName(name: "configuration_items")
    ]
    public JObject Configurations { get; set; } = [];
}

internal partial class AggConfigEngineModel
{
    internal async Task<AggConfigEngineModel> Load(
        string aggregateCode,
        PostgreHelper pgHelper,
        Caching caching,
        CancellationToken cancellationToken = default
    )
    {
        aggregateCode.ThrowIfNullOrWhitespace(paramName: nameof(aggregateCode));

        var result =
            await caching.GetOrSetAsync(
                key: aggregateCode,
                valueFactory: async () =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_get_aggr_config_by_code",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_gc_code",
                                    Value = aggregateCode,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: cancellationToken
                    );

                    return new AggConfigEngineModel
                    {
                        Code = aggregateCode,
                        Configurations = JObject.Parse(
                            json: data as string
                                ?? throw new DataException(
                                    s: $"Aggregate configuration '{aggregateCode}' not found."
                                )
                        ),
                    };
                },
                expirationRenewal: true
            )
            ?? throw new DataException(s: $"Aggregate configuration '{aggregateCode}' not found.");

        return result;
    }
}
