namespace IDC.AggrMapping.Utilities.Helpers;

/// <summary>
/// Provides utility methods for system logging operations and file management. Supports both Windows and Linux systems.
/// </summary>
/// <remarks>
/// A static class that handles system logging operations including file path management, log file filtering,
/// file size formatting, and log entry parsing.
///
/// Core Features:
/// - Log file path management
/// - Date-based log file filtering
/// - Human-readable file size formatting
/// - Log entry parsing and grouping
///
/// Configuration Example:
/// <code>
/// {
///   "Logging": {
///     "LogLevel": {
///       "Default": "Information",
///       "Microsoft.AspNetCore": "Warning"
///     },
///     "LogDirectory": "logs",
///     "RetentionDays": 30
///   }
/// }
/// </code>
///
/// Example Usage:
/// <code>
/// // Get log files for last 7 days
/// var baseDir = AppDomain.CurrentDomain.BaseDirectory;
/// var logPath = SystemLoggingLogic.GetFullLogPath(
///     baseDirectory: baseDir,
///     logDirectory: "logs"
/// );
/// var files = SystemLoggingLogic.GetLogFilesByDateRange(
///     fullPath: logPath,
///     startTime: DateTime.Today.AddDays(-7),
///     endTime: DateTime.Today
/// );
///
/// // Process log entries
/// var entries = SystemLoggingLogic.GetLogEntries(
///     logFiles: files,
///     startTime: DateTime.Today.AddDays(-1),
///     endTime: DateTime.Today
/// );
/// </code>
///
/// > [!NOTE]
/// > Log files follow the naming pattern "logs-YYYY-MM-DD.txt"
///
/// > [!TIP]
/// > Use date range filtering to optimize log processing performance
///
/// > [!IMPORTANT]
/// > Ensure proper file system permissions for log directory access
///
/// > [!CAUTION]
/// > Large log files may impact memory usage during processing
/// </remarks>
/// <example>
/// <code>
/// // Complete workflow example
/// var logPath = SystemLoggingLogic.GetFullLogPath(
///     baseDirectory: "C:/app",
///     logDirectory: "logs"
/// );
///
/// var files = SystemLoggingLogic.GetLogFilesByDateRange(
///     fullPath: logPath,
///     startTime: DateTime.Today.AddDays(-1),
///     endTime: DateTime.Today
/// );
///
/// foreach (var file in files)
/// {
///     var info = SystemLoggingLogic.CreateFileInfo(
///         file: file,
///         requestScheme: "https",
///         requestHost: "api.example.com"
///     );
///     Console.WriteLine($"Log file: {info.Name}, Size: {info.Size}");
/// }
/// </code>
/// </example>
/// <seealso cref="FileInfo"/>
/// <seealso cref="Path"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.file">File Class Documentation</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/">ASP.NET Core Logging</seealso>
public static class SystemLoggingLogic
{
    /// <summary>
    /// Gets the full path for log directory based on base directory and log directory name.
    /// </summary>
    /// <param name="baseDirectory">
    /// Base directory path where logs will be stored.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory"/>
    /// </param>
    /// <param name="logDirectory">
    /// Log directory name or relative path.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory"/>
    /// </param>
    /// <returns>
    /// Full normalized path to log directory.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.path"/>
    /// </returns>
    /// <remarks>
    /// Combines and normalizes the base directory path with log directory name.
    /// Handles path separators and relative paths automatically.
    ///
    /// Path Resolution Rules:
    /// - Removes leading slashes from logDirectory
    /// - Normalizes path separators for current OS
    /// - Resolves relative path segments
    /// - Returns absolute path
    ///
    /// Example Response:
    /// <code>
    /// // Windows
    /// Input:  baseDirectory: "C:\\app", logDirectory: "logs"
    /// Output: "C:\\app\\logs"
    ///
    /// // Linux
    /// Input:  baseDirectory: "/var/app", logDirectory: "/logs"
    /// Output: "/var/app/logs"
    ///
    /// // With relative paths
    /// Input:  baseDirectory: "./app", logDirectory: "../logs"
    /// Output: "/absolute/path/to/logs"
    /// </code>
    ///
    /// > [!NOTE]
    /// > Returns normalized path using OS-specific directory separators
    ///
    /// > [!TIP]
    /// > Use relative paths from baseDirectory for portable code
    ///
    /// > [!IMPORTANT]
    /// > Ensure write permissions exist for returned path
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage
    /// var path = SystemLoggingLogic.GetFullLogPath(
    ///     baseDirectory: AppDomain.CurrentDomain.BaseDirectory,
    ///     logDirectory: "logs"
    /// );
    ///
    /// // With relative paths
    /// var relativePath = SystemLoggingLogic.GetFullLogPath(
    ///     baseDirectory: "./app",
    ///     logDirectory: "../logs"
    /// );
    ///
    /// // With custom base directory
    /// var customPath = SystemLoggingLogic.GetFullLogPath(
    ///     baseDirectory: "/var/myapp",
    ///     logDirectory: "storage/logs"
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="Path.GetFullPath(string)"/>
    /// <seealso cref="Path.Combine(string, string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath">Path.GetFullPath Documentation</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/io/file-path-formats">File Path Formats</seealso>
    public static string GetFullLogPath(string baseDirectory, string logDirectory) =>
        Path.GetFullPath(
            path: Path.Combine(path1: baseDirectory, path2: logDirectory.TrimStart('/', '\\'))
        );

    /// <summary>
    /// Retrieves log files within specified date range.
    /// </summary>
    /// <param name="fullPath">
    /// Full path to log directory.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory"/>
    /// </param>
    /// <param name="startTime">
    /// Start date for filtering.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.datetime"/>
    /// </param>
    /// <param name="endTime">
    /// End date for filtering.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.datetime"/>
    /// </param>
    /// <returns>
    /// Array of FileInfo objects for matching log files, ordered by LastWriteTime.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo"/>
    /// </returns>
    /// <remarks>
    /// Searches for log files matching pattern "logs-*.txt" and filters by LastWriteTime within the specified date range.
    /// Files are returned in ascending order by LastWriteTime.
    ///
    /// Features:
    /// - Pattern matching for log files
    /// - Date range filtering
    /// - Chronological ordering
    /// - Full file metadata access
    ///
    /// Example Response:
    /// <code>
    /// [
    ///   {
    ///     "FullName": "C:/logs/logs-2024-01-01.txt",
    ///     "Name": "logs-2024-01-01.txt",
    ///     "Length": 1048576,
    ///     "LastWriteTime": "2024-01-01T23:59:59",
    ///     "CreationTime": "2024-01-01T00:00:00"
    ///   },
    ///   {
    ///     "FullName": "C:/logs/logs-2024-01-02.txt",
    ///     "Name": "logs-2024-01-02.txt",
    ///     "Length": 2097152,
    ///     "LastWriteTime": "2024-01-02T23:59:59",
    ///     "CreationTime": "2024-01-02T00:00:00"
    ///   }
    /// ]
    /// </code>
    ///
    /// > [!NOTE]
    /// > Only files matching pattern "logs-*.txt" are included in results
    ///
    /// > [!TIP]
    /// > Use narrow date ranges to improve search performance
    ///
    /// > [!IMPORTANT]
    /// > Ensure read permissions exist for the log directory
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with absolute path
    /// var files = SystemLoggingLogic.GetLogFilesByDateRange(
    ///     fullPath: "C:/logs",
    ///     startTime: DateTime.Today.AddDays(-7),
    ///     endTime: DateTime.Today
    /// );
    ///
    /// // Using with dynamic path
    /// var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    /// var files = SystemLoggingLogic.GetLogFilesByDateRange(
    ///     fullPath: logPath,
    ///     startTime: DateTime.Today.AddDays(-1),
    ///     endTime: DateTime.Today
    /// );
    ///
    /// foreach (var file in files)
    /// {
    ///     Console.WriteLine($"Log: {file.Name}, Size: {file.Length} bytes");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetLogEntries"/>
    /// <seealso cref="CreateFileInfo"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.getfiles">Directory.GetFiles Method</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/io/file-path-formats">File Path Formats</seealso>
    public static FileInfo[] GetLogFilesByDateRange(
        string fullPath,
        DateTime startTime,
        DateTime endTime
    ) =>
        [
            .. Directory
                .GetFiles(path: fullPath, searchPattern: "logs-*.txt")
                .Select(selector: f => new FileInfo(fileName: f))
                .Where(predicate: f =>
                    f.LastWriteTime.Date >= startTime.Date && f.LastWriteTime.Date <= endTime.Date
                )
                .OrderBy(keySelector: f => f.LastWriteTime),
        ];

    /// <summary>
    /// Formats file size to human-readable string with appropriate unit.
    /// </summary>
    /// <param name="length">
    /// File size in bytes.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.int64"/>
    /// </param>
    /// <returns>
    /// Formatted string with appropriate size unit (B, KB, MB, GB, TB).
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string"/>
    /// </returns>
    /// <remarks>
    /// Automatically converts byte lengths to the most appropriate unit for human readability.
    /// Uses standard binary prefixes (1024 bytes = 1 KB).
    ///
    /// Conversion Rules:
    /// - Bytes (B): 0-1023 bytes
    /// - Kilobytes (KB): 1024 bytes - 1,048,575 bytes
    /// - Megabytes (MB): 1,048,576 bytes - 1,073,741,823 bytes
    /// - Gigabytes (GB): 1,073,741,824 bytes - 1,099,511,627,775 bytes
    /// - Terabytes (TB): 1,099,511,627,776 bytes and above
    ///
    /// Example outputs:
    /// <code>
    /// FormatFileSize(500)         // Returns "500 B"
    /// FormatFileSize(1536)        // Returns "1.50 KB"
    /// FormatFileSize(2359296)     // Returns "2.25 MB"
    /// FormatFileSize(3221225472)  // Returns "3.00 GB"
    /// FormatFileSize(4947802324992) // Returns "4.50 TB"
    /// </code>
    ///
    /// > [!NOTE]
    /// > All values are rounded to 2 decimal places except for bytes
    ///
    /// > [!TIP]
    /// > Use this method for displaying file sizes in user interfaces and logs
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage
    /// var fileSize = SystemLoggingLogic.FormatFileSize(
    ///     length: 1234567
    /// );
    /// Console.WriteLine($"File size: {fileSize}"); // Output: "File size: 1.18 MB"
    ///
    /// // With FileInfo
    /// var fileInfo = new FileInfo("large-file.dat");
    /// var size = SystemLoggingLogic.FormatFileSize(
    ///     length: fileInfo.Length
    /// );
    /// Console.WriteLine($"Size of {fileInfo.Name}: {size}");
    /// </code>
    /// </example>
    /// <seealso cref="CreateFileInfo"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo.length">FileInfo.Length Property</seealso>
    /// <seealso href="https://en.wikipedia.org/wiki/Binary_prefix">Binary Prefix Standards</seealso>
    public static string FormatFileSize(long length) =>
        length switch
        {
            < 0x400 => $"{length} B",
            < 0x400 * 0x400 => $"{length / 1024.0:N2} KB",
            < 0x400 * 0x400 * 0x400 => $"{length / (1024.0 * 0x400):N2} MB",
            < 1024L * 0x400 * 0x400 * 0x400 => $"{length / (1024.0 * 0x400 * 0x400):N2} GB",
            _ => $"{length / (1024.0 * 0x400 * 0x400 * 0x400):N2} TB",
        };

    /// <summary>
    /// Creates file information object with URL.
    /// </summary>
    /// <param name="file">
    /// FileInfo object containing file metadata.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo"/>
    /// </param>
    /// <param name="requestScheme">
    /// HTTP scheme (http/https) for generating download URL.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httprequest.scheme"/>
    /// </param>
    /// <param name="requestHost">
    /// Host name with optional port for generating download URL.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httprequest.host"/>
    /// </param>
    /// <returns>
    /// Anonymous object containing file details and download URL.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/anonymous-types"/>
    /// </returns>
    /// <remarks>
    /// Creates a structured object containing file metadata and a download URL for accessing the file.
    ///
    /// The returned object includes:
    /// - File name
    /// - Formatted file size (B, KB, MB, GB, TB)
    /// - Creation timestamp
    /// - Last modification timestamp
    /// - Full download URL
    ///
    /// Example Response:
    /// <code>
    /// {
    ///   "Name": "logs-2024-01-01.txt",
    ///   "Size": "1.5 MB",
    ///   "Created": "2024-01-01T00:00:00",
    ///   "Modified": "2024-01-01T23:59:59",
    ///   "URL": "https://example.com/logs/logs-2024-01-01.txt"
    /// }
    /// </code>
    ///
    /// > [!NOTE]
    /// > File size is automatically formatted to the most appropriate unit
    ///
    /// > [!TIP]
    /// > The URL is constructed using the scheme and host from the current request
    ///
    /// > [!IMPORTANT]
    /// > Ensure the generated URL is accessible through your web server configuration
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with local file
    /// var fileInfo = new FileInfo("logs/app.log");
    /// var info = SystemLoggingLogic.CreateFileInfo(
    ///     file: fileInfo,
    ///     requestScheme: "https",
    ///     requestHost: "api.example.com"
    /// );
    ///
    /// // Using with HttpContext
    /// var info = SystemLoggingLogic.CreateFileInfo(
    ///     file: fileInfo,
    ///     requestScheme: HttpContext.Request.Scheme,
    ///     requestHost: HttpContext.Request.Host.Value
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="FormatFileSize"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo">FileInfo Class Documentation</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/url-rewriting">URL Generation in ASP.NET Core</seealso>
    public static object CreateFileInfo(FileInfo file, string requestScheme, string requestHost) =>
        new
        {
            file.Name,
            Size = FormatFileSize(file.Length),
            Created = file.CreationTime,
            Modified = file.LastWriteTime,
            URL = $"{requestScheme}://{requestHost}/logs/{file.Name}",
        };

    /// <summary>
    /// Extracts log entries from files within specified date range.
    /// </summary>
    /// <param name="logFiles">
    /// Array of log files to process.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo"/>
    /// </param>
    /// <param name="startTime">
    /// Start time for filtering entries.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.datetime"/>
    /// </param>
    /// <param name="endTime">
    /// End time for filtering entries.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.datetime"/>
    /// </param>
    /// <returns>
    /// List of parsed log entries as dynamic objects.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1"/>
    /// </returns>
    /// <remarks>
    /// Reads and parses log entries from multiple files, filtering by timestamp.
    ///
    /// Features:
    /// - Processes multiple log files concurrently
    /// - Filters entries by timestamp range
    /// - Handles both simple and detailed log formats
    /// - Preserves entry order within files
    ///
    /// Example Response:
    /// <code>
    /// [
    ///   {
    ///     "Timestamp": "2024-01-01T12:00:00",
    ///     "Level": "INFO",
    ///     "Type": "System",
    ///     "Message": "Application started"
    ///   },
    ///   {
    ///     "Timestamp": "2024-01-01T12:01:00",
    ///     "Level": "ERROR",
    ///     "Type": "System.Exception",
    ///     "Message": "Operation failed",
    ///     "StackTrace": [
    ///       "at Method() in File.cs:line 10"
    ///     ]
    ///   }
    /// ]
    /// </code>
    ///
    /// > [!NOTE]
    /// > Invalid entries are automatically skipped during processing
    ///
    /// > [!TIP]
    /// > Use narrow date ranges to improve processing performance
    ///
    /// > [!IMPORTANT]
    /// > Ensure sufficient memory when processing large log files
    /// </remarks>
    /// <example>
    /// <code>
    /// var files = Directory
    ///     .GetFiles("logs")
    ///     .Select(f => new FileInfo(f))
    ///     .ToArray();
    ///
    /// var entries = SystemLoggingLogic.GetLogEntries(
    ///     logFiles: files,
    ///     startTime: DateTime.Today.AddDays(-1),
    ///     endTime: DateTime.Today
    /// );
    ///
    /// foreach (dynamic entry in entries)
    /// {
    ///     Console.WriteLine($"[{entry.Timestamp}] [{entry.Level}] {entry.Message}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="TryParseLogEntry"/>
    /// <seealso cref="GroupLogEntries"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-read-text-from-a-file">File Reading Best Practices</seealso>
    public static List<object> GetLogEntries(
        FileInfo[] logFiles,
        DateTime startTime,
        DateTime endTime
    )
    {
        var logEntries = new List<object>();

        foreach (var file in logFiles)
        {
            foreach (
                var entry in RegexAttributes
                    .LogEntrySplitter()
                    .Split(input: File.ReadAllText(path: file.FullName))
                    .Where(predicate: static e => !string.IsNullOrWhiteSpace(value: e))
            )
                if (
                    TryParseLogEntry(line: entry, entry: out var parsedEntry)
                    && parsedEntry.Timestamp >= startTime
                    && parsedEntry.Timestamp <= endTime
                )
                    logEntries.Add(item: parsedEntry);
        }

        return logEntries;
    }

    /// <summary>
    /// Groups log entries by date and hour.
    /// </summary>
    /// <param name="logEntries">
    /// Collection of log entries to be grouped.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1"/>
    /// </param>
    /// <returns>
    /// Object containing grouped log entries organized by date and hour.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/anonymous-types"/>
    /// </returns>
    /// <remarks>
    /// Organizes log entries hierarchically by date and hour, providing entry counts and detailed information.
    ///
    /// Features:
    /// - Groups entries by date
    /// - Subgroups by hour within each date
    /// - Maintains chronological order (newest first)
    /// - Preserves all entry details
    ///
    /// Example response:
    /// <code>
    /// {
    ///   "Items": [
    ///     {
    ///       "Date": "2024-01-01",
    ///       "Total": 100,
    ///       "Hours": [
    ///         {
    ///           "Hour": 23,
    ///           "Entries": [
    ///             {
    ///               "Timestamp": "2024-01-01T23:59:59",
    ///               "Level": "INFO",
    ///               "Type": "System",
    ///               "Message": "Application shutdown"
    ///             }
    ///           ]
    ///         }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// </code>
    ///
    /// > [!NOTE]
    /// > Entries are sorted in descending order (newest first) at both date and hour levels
    ///
    /// > [!TIP]
    /// > Use the Total property to quickly get entry counts without traversing Hours array
    /// </remarks>
    /// <example>
    /// <code>
    /// var entries = new List&lt;object&gt;
    /// {
    ///     new {
    ///         Timestamp = DateTime.Now,
    ///         Level = "INFO",
    ///         Message = "Test entry"
    ///     }
    /// };
    ///
    /// var grouped = SystemLoggingLogic.GroupLogEntries(
    ///     logEntries: entries
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="GetLogEntries"/>
    /// <seealso cref="TryParseLogEntry"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/linq/group-query-results">LINQ Grouping Operations</seealso>
    public static object GroupLogEntries(List<object> logEntries) =>
        logEntries
            .GroupBy(keySelector: static e => ((dynamic)e).Timestamp.Date)
            .OrderByDescending(keySelector: static g => g.Key)
            .Select(static dateGroup => new
            {
                Date = dateGroup.Key.ToString("yyyy-MM-dd"),
                Total = dateGroup.Count(),
                Hours = dateGroup
                    .GroupBy(keySelector: static e => ((dynamic)e).Timestamp.Hour)
                    .OrderByDescending(keySelector: static h => h.Key)
                    .Select(selector: static hourGroup => new
                    {
                        Hour = hourGroup.Key,
                        Entries = hourGroup
                            .OrderByDescending(keySelector: static e => ((dynamic)e).Timestamp)
                            .ToList(),
                    })
                    .ToList(),
            })
            .ToList();

    /// <summary>
    /// Attempts to parse a log entry line into a structured format.
    /// </summary>
    /// <param name="line">
    /// Raw log entry line to parse.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string"/>
    /// </param>
    /// <param name="entry">
    /// Output parameter containing the parsed log entry if successful.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#the-dynamic-type"/>
    /// </param>
    /// <returns>
    /// True if parsing was successful, false otherwise.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool"/>
    /// </returns>
    /// <remarks>
    /// Supports two distinct log entry formats with different parsing strategies:
    ///
    /// 1. Simple Format:
    /// <code>
    /// [Timestamp] [Level] Message
    ///
    /// Example:
    /// [2024-01-01 12:00:00] [INFO] Application started successfully
    ///
    /// Parsed Result:
    /// {
    ///     "Timestamp": "2024-01-01T12:00:00",
    ///     "Level": "INFO",
    ///     "Type": "",
    ///     "Message": "Application started successfully"
    /// }
    /// </code>
    ///
    /// 2. Detailed Format:
    /// <code>
    /// [Timestamp] [Level] Type: Message --> StackTrace
    ///
    /// Example:
    /// [2024-01-01 12:00:00] [ERROR] System.Exception: Database connection failed
    ///    --> at DataAccess.Connect() in DataAccess.cs:line 24
    ///    --> at Program.Main() in Program.cs:line 12
    ///
    /// Parsed Result:
    /// {
    ///     "Timestamp": "2024-01-01T12:00:00",
    ///     "Level": "ERROR",
    ///     "Type": "System.Exception",
    ///     "Message": "Database connection failed",
    ///     "StackTrace": [
    ///         "at DataAccess.Connect() in DataAccess.cs:line 24",
    ///         "at Program.Main() in Program.cs:line 12"
    ///     ]
    /// }
    /// </code>
    ///
    /// > [!NOTE]
    /// > Invalid log entries return false without throwing exceptions
    ///
    /// > [!TIP]
    /// > Check entry.Type to determine if it's a simple or detailed log entry
    ///
    /// > [!IMPORTANT]
    /// > Timestamp must be in format: yyyy-MM-dd HH:mm:ss
    ///
    /// > [!CAUTION]
    /// > Large stack traces may impact memory usage
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple log parsing
    /// var simpleLog = "[2024-01-01 12:00:00] [INFO] Server started";
    /// if (SystemLoggingLogic.TryParseLogEntry(
    ///     line: simpleLog,
    ///     entry: out var simpleEntry
    /// ))
    /// {
    ///     Console.WriteLine($"Level: {simpleEntry.Level}");
    ///     Console.WriteLine($"Message: {simpleEntry.Message}");
    /// }
    ///
    /// // Detailed log parsing
    /// var detailedLog = "[2024-01-01 12:00:00] [ERROR] System.Exception: Error occurred";
    /// if (SystemLoggingLogic.TryParseLogEntry(
    ///     line: detailedLog,
    ///     entry: out var detailedEntry
    /// ))
    /// {
    ///     Console.WriteLine($"Type: {detailedEntry.Type}");
    ///     Console.WriteLine($"Message: {detailedEntry.Message}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="DateTime.Parse(string)"/>
    /// <seealso cref="RegexAttributes"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex">Regex Class</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions">Regular Expressions</seealso>
    private static bool TryParseLogEntry(string line, out dynamic entry)
    {
        entry = null!;
        try
        {
            if (!line.Contains("Type:"))
            {
                var simpleMatch = RegexAttributes.SimpleLogEntry().Match(input: line);

                if (simpleMatch.Success)
                {
                    entry = new
                    {
                        Timestamp = DateTime.Parse(
                            s: simpleMatch.Groups[0b1].Value,
                            provider: System.Globalization.CultureInfo.InvariantCulture
                        ),
                        Level = simpleMatch.Groups[0b10].Value,
                        Type = string.Empty,
                        Message = simpleMatch.Groups[0b11].Value.Trim(),
                    };
                    return true;
                }
            }
            else
            {
                var detailedMatch = RegexAttributes.DetailedLogEntry().Match(input: line);

                if (detailedMatch.Success)
                {
                    var stackTrace = detailedMatch
                        .Groups[0b101]
                        .Value.Split(
                            separator: "\n   --> ",
                            options: StringSplitOptions.RemoveEmptyEntries
                        )
                        .Select(selector: static s =>
                            s.Trim().Replace(oldValue: "--> ", newValue: "")
                        )
                        .Where(predicate: static s => !string.IsNullOrWhiteSpace(value: s))
                        .ToList();

                    entry = new
                    {
                        Timestamp = DateTime.Parse(
                            s: detailedMatch.Groups[0b1].Value,
                            provider: System.Globalization.CultureInfo.InvariantCulture
                        ),
                        Level = detailedMatch.Groups[0b10].Value,
                        Type = detailedMatch.Groups[0b11].Value,
                        Message = detailedMatch.Groups[0b100].Value,
                        StackTrace = stackTrace,
                    };
                    return true;
                }
            }
        }
        catch
        {
            // Invalid log entry format
        }

        return false;
    }
}
