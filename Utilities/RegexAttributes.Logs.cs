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
    [GeneratedRegex(@"(?=\[\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\])", RegexOptions.Singleline)]
    public static partial Regex LogEntrySplitter();

    /// <summary>
    /// Parses a log entry into its components.
    /// </summary>
    /// <returns>
    /// A regular expression pattern that matches log entries.
    /// </returns>
    [GeneratedRegex(@"^\[(.*?)\] \[(.*?)\] (.+)$", RegexOptions.Singleline)]
    public static partial Regex SimpleLogEntry();

    /// <summary>
    /// Parses a detailed log entry into its components.
    /// </summary>
    /// <returns>
    /// A regular expression pattern that matches log entries.
    /// </returns>
    [GeneratedRegex(
        @"\[(.*?)\] \[(.*?)\] Type: (.*?)[\r\n]+Message: (.*?)[\r\n]+StackTrace:[\r\n]+((?:   --> .*(?:\r?\n|$))*)",
        RegexOptions.Singleline
    )]
    public static partial Regex DetailedLogEntry();
}
