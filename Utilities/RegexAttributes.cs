using System.Text.RegularExpressions;

namespace IDC.AggrMapping.Utilities;

/// <summary>
/// Provides a collection of generated regular expression patterns for common validation scenarios.
/// </summary>
/// <remarks>
/// This class contains source-generated regular expressions using the <see cref="GeneratedRegexAttribute"/>
/// to optimize performance by compiling patterns at build time.
///
/// Example usage:
/// <example>
/// <code>
/// // Validate email format
/// var isValidEmail = RegexAttributes.EmailPattern().IsMatch("user@example.com");
///
/// // Validate phone number
/// var isValidPhone = RegexAttributes.PhonePattern().IsMatch("+1-234-567-8900");
/// </code>
/// </example>
///
/// > [!NOTE]
/// > All patterns are compiled at build time for optimal runtime performance
///
/// > [!TIP]
/// > Use these patterns instead of creating new Regex instances for better performance
///
/// > [!IMPORTANT]
/// > These patterns are designed for common scenarios and may need adjustment for specific requirements
/// </remarks>
/// <seealso cref="GeneratedRegexAttribute"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex">Regex Class</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.generatedregexattribute">GeneratedRegexAttribute Class</seealso>
public static partial class RegexAttributes { }
