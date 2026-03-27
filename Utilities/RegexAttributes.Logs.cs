using System.Text.RegularExpressions;

namespace IDC.AggrMapping.Utilities;

/// <summary>
/// Contains regular expression patterns for splitting log entries and parsing log entries.
/// </summary>
public static partial class RegexAttributes
{
    /// <summary>
    /// Splits a log entry into its components.
    /// </summary>
    /// <returns>
    /// A regular expression pattern that matches log entries.
    /// </returns>
    [GeneratedRegex(pattern: @"(?=\[\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\])", options: RegexOptions.Singleline)]
    public static partial Regex LogEntrySplitter();

    /// <summary>
    /// Parses a log entry into its components.
    /// </summary>
    /// <returns>
    /// A regular expression pattern that matches log entries.
    /// </returns>
    [GeneratedRegex(pattern: @"^\[(.*?)\] \[(.*?)\] (.+)$", options: RegexOptions.Singleline)]
    public static partial Regex SimpleLogEntry();

    /// <summary>
    /// Parses a detailed log entry into its components.
    /// </summary>
    /// <returns>
    /// A regular expression pattern that matches log entries.
    /// </returns>
    [GeneratedRegex(
        pattern: @"\[(.*?)\] \[(.*?)\] Type: (.*?)[\r\n]+Message: (.*?)[\r\n]+StackTrace:[\r\n]+((?:   --> .*(?:\r?\n|$))*)",
        options: RegexOptions.Singleline
    )]
    public static partial Regex DetailedLogEntry();
}
