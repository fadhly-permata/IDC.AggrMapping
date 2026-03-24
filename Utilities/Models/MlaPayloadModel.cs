using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Represents a model for MlaPayload.
/// </summary>
public partial class MlaPayloadModel : BaseModel<MlaPayloadModel>
{
    /// <summary>
    ///     Gets or sets the flow code.
    /// </summary>
    [
        JsonProperty(propertyName: "flow_code"),
        JsonPropertyName(name: "flow_code"), // <-- paksa tampilin property name di swagger sesuai dengan yang tertulis
        Required(AllowEmptyStrings = false)
    ]
    public string FlowCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the conf maptable.
    /// </summary>
    [JsonProperty(propertyName: "confMaptable")]
    public List<string> ConfMaptable { get; set; } = [];

    /// <summary>
    ///     Gets or sets the conf agg map code.
    /// </summary>
    [JsonProperty(propertyName: "confAggMapCode")]
    public string ConfAggMapCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the data.
    /// </summary>
    [JsonProperty(propertyName: "data")]
    public JArray Data { get; set; } = [];
}

public partial class MlaPayloadModel
{
    internal InsertAndAggregatePayloadModel CastToAggregatePayload(
        InsertAndAggregatePayloadModel.OperationTypes operationTypes,
        int dataIndex
    )
    {
        if (Data == null || Data.Count == 0 || !Data.HasValues)
            throw new DataException(
                s: "Casting to AggregateAndInsertPayloadModel failed because Data is empty."
            );

        if (dataIndex < 0 || dataIndex >= Data.Count)
            throw new DataException(
                s: "Casting to AggregateAndInsertPayloadModel failed because dataIndex is out of range."
            );

        if (Data[dataIndex] is JObject elementOfData)
            return new InsertAndAggregatePayloadModel()
            {
                Code = ConfAggMapCode,
                Data = elementOfData,
            }.ChangeOperationType(operationType: operationTypes);

        throw new DataException(
            s: "Casting to AggregateAndInsertPayloadModel failed because Data is not valid json object."
        );
    }

    internal void Validate(GlobalConfigurationModel configs)
    {
        // Validate FlowCode
        if (string.IsNullOrWhiteSpace(FlowCode))
            throw new ArgumentException("FlowCode cannot be empty or whitespace.");

        // Validate ConfMaptable
        if (ConfMaptable == null || ConfMaptable.Count == 0)
            throw new ArgumentException("ConfMaptable cannot be null or empty.");

        if (ConfMaptable.Count > configs.MaxMapCount)
            throw new ArgumentException(
                $"ConfMaptable cannot have more than {configs.MaxMapCount} items."
            );

        // Validate Data array
        if (Data == null || !Data.Any())
            throw new ArgumentException("Data cannot be null or empty.");

        if (Data.Count > configs.MaxDataPayload)
            throw new ArgumentException(
                $"Data cannot have more than {configs.MaxDataPayload} items."
            );
    }
}
