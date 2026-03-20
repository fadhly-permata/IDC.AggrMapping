using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;
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
    public object Data { get; set; } = new();
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

    internal async Task Validate()
    {
        Code.ThrowIfNullOrWhitespace(paramName: nameof(Code));
        Data.ThrowIfNull(paramName: nameof(Data));

        if (Data is null)
            throw new DataException("Data can not be null.");

        if (Data is JArray array && array.Count == 0)
            throw new DataException("Data can not be empty.");
        else if (Data is JObject obj && obj.Count == 0)
            throw new DataException("Data can not be empty.");

        await Task.CompletedTask;
    }
}
