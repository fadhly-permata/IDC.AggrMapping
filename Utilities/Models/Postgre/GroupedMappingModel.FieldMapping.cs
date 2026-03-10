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
    /// Gets or sets a value indicating whether the field is a primary key.
    /// </summary>
    [
        JsonProperty(propertyName: "is_primary_key"),
        JsonPropertyName(name: "is_primary_key"),
        Required
    ]
    public bool IsPrimaryKey { get; set; } = false;

    /// <summary>
    ///     Gets or sets the data type of the field.
    /// </summary>
    [JsonProperty(propertyName: "data_type"), JsonPropertyName(name: "data_type"), Required]
    public string DataType { get; set; } = "unknown";

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

    /// <summary>
    ///     Returns a value indicating whether to add a quote to the query.
    /// </summary>
    /// <returns>
    ///     A value indicating whether to add a quote to the query.
    /// </returns>
    public bool RequiresQuotation()
    {
        return DataType?.ToLower() switch
        {
            "varchar" or "text" or "character varying" => true,
            "char" or "character" or "bpchar" => true,
            "date" or "timestamp" or "timestamp with time zone" or "timestamp without time zone" =>
                true,
            "time" or "time with time zone" or "time without time zone" => true,
            "interval" => true,
            "json" or "jsonb" => true,
            "uuid" => true,
            "inet" or "cidr" or "macaddr" => true,
            "money" => true,
            "bytea" => true,
            _ => false,
        };
    }
}
