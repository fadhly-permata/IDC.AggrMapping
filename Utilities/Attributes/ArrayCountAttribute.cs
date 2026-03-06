using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Attributes;

/// <summary>
///     Attribute for validating the count of an array property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ArrayCountAttribute : ValidationAttribute
{
    /// <summary>
    ///     Gets or sets the minimum count of items in the array.
    /// </summary>
    public int MinCount { get; set; } = 0;

    /// <summary>
    ///     Gets or sets the maximum count of items in the array.
    /// </summary>
    public int MaxCount { get; set; } = int.MaxValue;

    /// <summary>
    ///     Gets or sets the maximum count of items in the array.
    /// </summary>
    public Func<object?, ValidationContext, int>? MaxCountDelegate { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether null values are allowed.
    /// </summary>
    public bool AllowNull { get; set; } = false;

    /// <summary>
    ///     Validates the count of an array property.
    /// </summary>
    /// <param name="value">
    ///     The value to validate.
    /// </param>
    /// <param name="validationContext">
    ///     The validation context.
    /// </param>
    /// <returns>
    ///     The validation result.
    /// </returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return AllowNull
                ? ValidationResult.Success
                : new ValidationResult($"{validationContext.DisplayName} cannot be null.");

        if (value is ICollection<string> collection)
        {
            if (collection.Count < MinCount)
                return new ValidationResult(
                    $"{validationContext.DisplayName} must have at least {MinCount} items."
                );

            if (collection.Count > MaxCount)
                return new ValidationResult(
                    $"{validationContext.DisplayName} cannot have more than {MaxCount} items."
                );
        }

        if (value is JArray jArray)
        {
            if (jArray.Count < MinCount)
                return new ValidationResult(
                    $"{validationContext.DisplayName} must have at least {MinCount} items."
                );

            if (jArray.Count > MaxCount)
                return new ValidationResult(
                    $"{validationContext.DisplayName} cannot have more than {MaxCount} items."
                );
        }

        return ValidationResult.Success;
    }
}
