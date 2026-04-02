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
public partial class MultiLayerPayloadModel : BaseModel<MultiLayerPayloadModel>
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
    ///     Gets or sets the wftype.
    /// </summary>
    [JsonProperty(propertyName: "wftype"), JsonPropertyName(name: "wftype")]
    public string? WorkflowType { get; set; } = string.Empty;

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

public partial class MultiLayerPayloadModel
{
    internal UpsertAndAggregatePayloadModel CastToAggregatePayload(
        string batchId,
        int totalProcess,
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

        if (Data[index: dataIndex] is JObject elementOfData)
            return new UpsertAndAggregatePayloadModel()
            {
                BatchId = batchId,
                ProcessIndex = dataIndex + 1,
                TotalProcess = totalProcess,
                Code = ConfAggMapCode,
                Data = elementOfData,
            }.ChangeOperationType(
                operationType: UpsertAndAggregatePayloadModel.OperationTypes.Aggregation
            );

        throw new DataException(
            s: "Casting to AggregateAndInsertPayloadModel failed because Data is not valid json object."
        );
    }

    internal UpsertAndAggregatePayloadModel CastToUpsertPayload(string batchId, string mapCode)
    {
        if (Data == null || Data.Count == 0 || !Data.HasValues)
            throw new DataException(
                s: "Casting to UpsertAndInsertPayloadModel failed because Data is empty."
            );

        return new UpsertAndAggregatePayloadModel()
        {
            BatchId = batchId,
            ProcessIndex = 1,
            TotalProcess = Data.Count,
            Code = mapCode,
            Data = Data,
        }.ChangeOperationType(
            operationType: UpsertAndAggregatePayloadModel.OperationTypes.InsertData
        );
    }

    internal void Validate(GlobalConfigurationModel configs)
    {
        // Validate FlowCode
        if (string.IsNullOrWhiteSpace(value: FlowCode))
            throw new ArgumentException(message: "FlowCode cannot be empty or whitespace.");

        // Validate Workflow Type if FlowCode starts with W
        if (FlowCode[..1] == "W" && string.IsNullOrWhiteSpace(value: WorkflowType))
            throw new ArgumentException(
                message: "When calling Workflow, WorkflowType cannot be empty or whitespace."
            );

        // Validate ConfMaptable
        if (ConfMaptable == null || ConfMaptable.Count == 0)
            throw new ArgumentException(message: "ConfMaptable cannot be null or empty.");

        // Validate ConfMaptable
        if (ConfMaptable.Count > configs.MaxMapCount)
            throw new ArgumentException(
                message: $"ConfMaptable cannot have more than {configs.MaxMapCount} items."
            );

        // Validate Data array
        if (Data == null || !Data.Any())
            throw new ArgumentException(message: "Data cannot be null or empty.");

        if (Data.Count > configs.MaxDataPayload)
            throw new ArgumentException(
                message: $"Data cannot have more than {configs.MaxDataPayload} items."
            );
    }
}
