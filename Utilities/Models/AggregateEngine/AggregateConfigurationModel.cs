using System.ComponentModel.DataAnnotations;
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
    public string AggregateCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the data.
    /// </summary>
    [
        JsonProperty(propertyName: "configuration_items"),
        JsonPropertyName(name: "configuration_items")
    ]
    public JObject ConfigurationItems { get; set; } = [];
}

internal partial class AggregateConfigurationModel
{
    public async Task<AggregateConfigurationModel> LoadDummy(
        string aggregateCode,
        PostgreHelper pgHelper,
        Caching caching,
        CancellationToken cancellationToken = default
    )
    {
        aggregateCode.ThrowIfNullOrWhitespace(nameof(aggregateCode));

        var result = caching.GetOrSetAsync(
            key: aggregateCode,
            valueFactory: async () =>
                await pgHelper.GetAggregateConfigurationAsync(
                    aggregateCode: aggregateCode,
                    cancellationToken: cancellationToken
                ),
            expirationRenewal: true
        );
        return this;
    }
}
