using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using System.Text.Json.Serialization;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Represents a model for AggrPayload.
/// </summary>
public partial class InsertAndAggregatePayloadModel : BaseModel<InsertAndAggregatePayloadModel>
{
    /// <summary>
    ///     Gets or sets the batch id.
    /// </summary>
    [
        JsonProperty(propertyName: "batch_id"),
        JsonPropertyName(name: "batch_id"),
        Required(AllowEmptyStrings = false)
    ]
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the total items.
    /// </summary>
    [
        JsonProperty(propertyName: "total_items"),
        JsonPropertyName(name: "total_items"),
        Range(minimum: 1, maximum: 100, ErrorMessage = "Total items must be between 1 and 100.")
    ]
    public int TotalItems { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the process index.
    /// </summary>
    [
        JsonProperty(propertyName: "process_index"),
        JsonPropertyName(name: "process_index"),
        Range(minimum: 1, maximum: 100, ErrorMessage = "Process index must be between 1 and 100.")
    ]
    public int ProcessIndex { get; set; } = 1;

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

    /// <summary>
    ///     Gets or sets the log message.
    /// </summary>
    internal StringBuilder LogMessage { get; set; } = new();
}

public partial class InsertAndAggregatePayloadModel
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

    internal InsertAndAggregatePayloadModel ChangeOperationType(OperationTypes operationType)
    {
        ArgumentNullException.ThrowIfNull(argument: operationType);

        OperationType = operationType;
        return this;
    }

    internal async Task Validate()
    {
        Code.ThrowIfNullOrWhitespace(paramName: nameof(Code));
        Data.ThrowIfNull(paramName: nameof(Data));

        BatchId.ThrowIfNullOrWhitespace(paramName: nameof(BatchId));
        if (TotalItems < 1 || TotalItems > 100)
            throw new DataException("Total items must be between 1 and 100.");

        if (ProcessIndex < 1 || ProcessIndex > 100)
            throw new DataException("Process index must be between 1 and 100.");

        if (ProcessIndex > TotalItems)
            throw new DataException("Process index must be less than or equal to total items.");

        if (Data is null)
            throw new DataException("Data can not be null.");

        if (Data is JArray array && array.Count == 0)
            throw new DataException("Data can not be empty.");
        else if (Data is JObject obj && obj.Count == 0)
            throw new DataException("Data can not be empty.");

        await Task.CompletedTask;
    }
}
