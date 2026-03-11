using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Represents a model for AggrPayload.
/// </summary>
public partial class AggregateAndInsertPayloadModel : BaseModel<AggregateAndInsertPayloadModel>
{
    /// <summary>
    ///     Gets or sets the code.
    /// </summary>
    [
        JsonProperty(propertyName: "code"),
        JsonPropertyName(name: "code"),
        Required(AllowEmptyStrings = false),
    ]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the data.
    /// </summary>
    [JsonProperty(propertyName: "data"), JsonPropertyName(name: "data")]
    public JArray Data { get; set; } = [];
}

public partial class AggregateAndInsertPayloadModel
{
    internal enum OperationTypes
    {
        /// <summary>
        ///     Aggregation
        /// </summary>
        Aggregation,

        /// <summary>
        ///     Insert
        /// </summary>
        InsertData,
    }

    internal OperationTypes OperationType { get; private set; } = OperationTypes.Aggregation;

    internal AggregateAndInsertPayloadModel ChangeOperationType(OperationTypes operationType)
    {
        ArgumentNullException.ThrowIfNull(argument: operationType);

        OperationType = operationType;
        return this;
    }

    internal async Task Validate(
        PostgreHelper pgHelper,
        Caching caching,
        CancellationToken cancellationToken = default
    )
    {
        Code.ThrowIfNullOrWhitespace(paramName: nameof(Code));
        Data.ThrowIfNull(paramName: nameof(Data));

        var gcm = await new GlobalConfigurationModel().InitFromDatabase(
            pgHelper: pgHelper,
            caching: caching,
            cancellationToken: cancellationToken
        );

        if (Data.Count == 0)
            throw new ArgumentException("Data cannot be empty.");

        if (Data.Count > gcm.MaxDataPayload)
            throw new ArgumentException($"Data cannot have more than {gcm.MaxDataPayload} items.");

        foreach (var dataItem in Data)
        {
            if (dataItem is null)
                throw new ArgumentException("Each element on 'Data' cannot be null.");

            if (dataItem is JObject jObject && !jObject.ValidateJsonDepth(maxDepth: gcm.MaxDepth))
                throw new ArgumentException($"Data cannot have more than {gcm.MaxDepth} levels.");
        }
    }
}
