using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IDC.AggrMapping.Utilities.Models.Postgre;

/// <summary>
/// Represents a single mapping group with destination and field mappings.
/// </summary>
public class MappingGroup
{
    /// <summary>
    /// Gets or sets the destination database configuration.
    /// </summary>
    [JsonProperty(propertyName: "destination")]
    [Required]
    public DestinationConfig Destination { get; set; } = new();

    /// <summary>
    /// Gets or sets the operation type (UPSERT, INSERT, UPDATE, etc.).
    /// </summary>
    [JsonProperty(propertyName: "operation")]
    [Required]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of field mappings.
    /// </summary>
    [
        JsonProperty(propertyName: "field_mappings"),
        JsonPropertyName(name: "field_mappings"),
        Required,
        MinLength(length: 1)
    ]
    public List<FieldMapping> FieldMappings { get; set; } = [];
}
