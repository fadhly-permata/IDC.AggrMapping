using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IDC.AggrMapping.Utilities.Models.Postgre;

/// <summary>
/// Represents destination database configuration.
/// </summary>
public class DestinationConfig
{
    /// <summary>
    /// Gets or sets the database type (POSTGRES, MYSQL, etc.).
    /// </summary>
    [JsonProperty(propertyName: "db_type"), JsonPropertyName(name: "db_type"), Required]
    public string DbType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    [JsonProperty(propertyName: "db_name"), JsonPropertyName(name: "db_name"), Required]
    public string DbName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database schema.
    /// </summary>
    [JsonProperty(propertyName: "schema")]
    [Required]
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    [JsonProperty(propertyName: "table")]
    [Required]
    public string Table { get; set; } = string.Empty;
}
