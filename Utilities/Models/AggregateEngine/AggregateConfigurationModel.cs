using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;
using IDC.AggrMapping.Utilities.Data;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models.AggregateEngine;

internal partial class AggregateConfigurationModel : BaseModel<AggregateConfigurationModel>
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

internal partial class AggregateConfigurationModel
{
    internal async Task<AggregateConfigurationModel> Load(
        string aggregateCode,
        PostgreHelper pgHelper,
        Caching caching,
        CancellationToken cancellationToken = default
    )
    {
        aggregateCode.ThrowIfNullOrWhitespace(nameof(aggregateCode));

        var result =
            await caching.GetOrSetAsync(
                key: aggregateCode,
                valueFactory: async () =>
                    await pgHelper.GetAggregateConfigurationAsync(
                        aggregateCode: aggregateCode,
                        cancellationToken: cancellationToken
                    )
                    ?? throw new DataException(
                        $"Aggregate configuration '{aggregateCode}' not found."
                    ),
                expirationRenewal: true
            ) ?? throw new DataException($"Aggregate configuration '{aggregateCode}' not found.");

        return result;
    }
}
