using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
///     Class AggrConfigFe Model
/// </summary>
public class AggrConfigFeModel
{
    /// <summary>
    ///     Represents the unique identifier for an aggregation configuration.
    /// </summary>
    public class IdOnly
    {
        /// <summary>
        ///     The unique numeric identifier of the aggregation configuration.
        ///     Must be a positive integer greater than zero.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ID must be a positive integer greater than zero.")]
        public int Id { get; set; }
    }

    /// <summary>
    ///     Represents the unique identifier for an aggregation configuration
    ///     and the user associated with the operation
    /// </summary>
    public class IdAndUser : IdOnly
    {
        /// <summary>
        ///     The username or identifier of the user associated with the operation.
        ///
        ///     Must not be empty or whitespace.
        ///     Can only contain alphanumeric characters, spaces, and underscores
        /// </summary>
        [Required(ErrorMessage = "User is required", AllowEmptyStrings = false)]
        [RegularExpression(@"\S+", ErrorMessage = "User cannot be empty or whitespace")]
        public string User { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Represents a model for copying an aggregation configuration
    /// </summary>
    public class CopyData : IdAndUser
    {
        /// <summary>
        ///     The display name or title of the aggregation configuration.
        ///
        ///     Must not be empty or whitespace.
        ///     Can only contain alphanumeric characters, spaces, and underscores
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [RegularExpression(
            "^[a-zA-Z0-9 _]+$",
            ErrorMessage = "Name can only contain alphanumeric characters, spaces, and underscores"
        )]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Represents a model for rolling back an aggregation configuration
    /// </summary>
    public class CodeOnly
    {
        /// <summary>
        ///     The unique alphanumeric code that identifies the
        ///     aggregation configuration
        ///
        ///     Must be in format 'ACV' followed by numbers
        /// </summary>
        [Required(ErrorMessage = "Code is required", AllowEmptyStrings = false)]
        [RegularExpression(
            @"^ACV\d+$",
            ErrorMessage = "Code must be in format 'ACV' followed by numbers (e.g., ACV123)"
        )]
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Represents an approval action on an aggregation configuration
    /// </summary>
    public class AggrConfigApproval : IdAndUser
    {
        /// <summary>
        ///     The role of the user in the approval process, accepting only 'CHECKER' or 'APPROVER'
        /// </summary>
        [RegularExpression(
            pattern: "^(CHECKER|APPROVER)$",
            ErrorMessage = "Role must be either 'CHECKER' or 'APPROVER'"
        )]
        public string? Role { get; set; }

        /// <summary>
        ///     The approval action being taken, accepting only 'APPROVE' or 'REJECT'
        /// </summary>
        [RegularExpression(
            pattern: "^(APPROVE|REJECT)$",
            ErrorMessage = "Action must be either 'APPROVE' or 'REJECT'"
        )]
        public string? Action { get; set; }

        /// <summary>
        ///     Optional comments or notes regarding the approval decision
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    ///     Represents a model for inserting a new aggregation configuration
    /// </summary>
    public class Insert
    {
        /// <summary>
        ///     The username or identifier of the user associated with the operation.
        ///
        ///     Must not be empty or whitespace.
        ///     Can only contain alphanumeric characters, spaces, and underscores
        /// </summary>
        [Required(ErrorMessage = "User is required", AllowEmptyStrings = false)]
        [RegularExpression(pattern: @"\S+", ErrorMessage = "User cannot be empty or whitespace")]
        public string User { get; set; } = string.Empty;

        /// <summary>
        ///     The display name or title of the aggregation configuration.
        ///
        ///     Must not be empty or whitespace.
        ///     Can only contain alphanumeric characters, spaces, and underscores
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [RegularExpression(
            pattern: "^[a-zA-Z0-9 _]+$",
            ErrorMessage = "Name can only contain alphanumeric characters, spaces, and underscores"
        )]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     The description of the aggregation configuration
        /// </summary>
        public string? Desc { get; set; }

        /// <summary>
        ///     The type of aggregation configuration (response or request)
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        ///     The JSON representation of the aggregation configuration
        /// </summary>
        [JsonProperty(propertyName: "data_applied")]
        [JsonPropertyName(name: "data_applied")]
        public string DataApplied { get; set; } = string.Empty;

        /// <summary>
        ///     The JSON representation of the aggregation configuration
        /// </summary>
        [JsonProperty(propertyName: "json_list")]
        [JsonPropertyName(name: "json_list")]
        public JArray? JsonList { get; set; }

        /// <summary>
        ///     The JSON representation of the aggregation configuration
        /// </summary>
        [JsonProperty(propertyName: "json_condition")]
        [JsonPropertyName(name: "json_condition")]
        public JArray? JsonCondition { get; set; }

        /// <summary>
        ///     The JSON representation of the aggregation configuration
        /// </summary>
        [JsonProperty(propertyName: "final_config")]
        [JsonPropertyName(name: "final_config")]
        public JArray? ConfigFinal { get; set; }
    }

    /// <summary>
    ///     Represents a model for updating an aggregation configuration
    /// </summary>
    public class Update : Insert
    {
        /// <summary>
        ///     The unique numeric identifier of the aggregation configuration.
        ///     Must be a positive integer greater than zero.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ID must be a positive integer greater than zero.")]
        public int Id { get; set; }
    }
}
