using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IDC.AggrMapping.Utilities.Models.Postgre;

/// <summary>
/// Represents a single field mapping between source and destination.
/// </summary>
public class FieldMapping
{
    /// <summary>
    /// Gets or sets the source field name.
    /// </summary>
    [JsonProperty(propertyName: "source_field"), JsonPropertyName(name: "source_field"), Required]
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination column name.
    /// </summary>
    [
        JsonProperty(propertyName: "destination_column"),
        JsonPropertyName(name: "destination_column"),
        Required
    ]
    public string DestinationColumn { get; set; } = string.Empty;
}
