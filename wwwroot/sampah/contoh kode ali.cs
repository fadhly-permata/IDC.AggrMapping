using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using IDC.Template.Utilities;
using IDC.Template.Utilities.DI;
using IDC.Template.Utilities.Extensions;
using IDC.Template.Utilities.Helpers;
using IDC.Template.Utilities.Models.DynamicAggregate;
using IDC.Utilities;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.Template.Controllers;

/// <summary>
/// Request aggregate object for dynamic aggregation.
/// </summary>
/// <remarks>
/// This object is used to pass dynamic aggregation requests to the server.
/// The request object should contain the following properties:
/// - <see cref="AggName"/>: The name of the aggregation function to be executed.
/// - <see cref="Detail"/>: The detail object containing the request data.
/// </remarks>
public class ReqAggregate
{
    /// <summary>Gets or sets the name of the aggregation config</summary>
    /// <value>Config name</value>
    [JsonProperty("aggName")]
    public required string AggName { get; set; }

    /// <summary>Gets or sets the detail request</summary>
    /// <value>The json object containing the response from webservice</value>
    [JsonProperty("detail")]
    public required JObject Detail { get; set; }
}

{
    Aggname: sample1
    Detail: { 
	"isSuccess":true, 
	"description":"", 
	"data":[ 
		{ 
		"RatingDate":"2020-07-20T00:00:00", 
		"RatingSource":"LOS", 
		"RatingCode":"4", 
		"RemarkLine1":"Macet" 
		}, 
		{ 
		"RatingDate":"2020-07-19T00:00:00", 
		"RatingSource":"AUTO", 
		"RatingCode":"5", 
		"RemarkLine1":"Macet" 
		} 
	] 
} 
}


/// <summary>
/// Controller for managing demo operations
/// </summary>
/// <remarks>
/// Provides endpoints for system logging and other demo functionalities
/// </remarks>
/// <example>
/// <code>
/// var controller = new DemoController(new SystemLogging());
/// controller.LogInfo(message: "Test message");
/// </code>
/// </example>
[Route("api/[controller]")]
[ApiController]
public partial class Testing(
    SystemLogging systemLogging,
    Language language,
    AppConfigsHandler appConfigs,
    DynamicAggrConfigsHandler dynamicAggrConfigs
) : ControllerBase
{
    private const string CON_API_STATUS_FAILED = "api.status.failed";

    /// <summary>
    /// Restarts the current application process
    /// </summary>
    /// <remarks>
    /// This endpoint initiates a graceful shutdown and restart of the application.
    /// The process is restarted with the same executable path as the current process.
    ///
    /// > [!IMPORTANT]
    /// > This operation will cause temporary service interruption.
    ///
    /// > [!CAUTION]
    /// > Ensure all critical operations are completed before calling this endpoint.
    ///
    /// Example usage:
    /// <code>
    /// var response = await httpClient.PostAsync("api/Demo/RestartApps", null);
    /// var result = await response.Content.ReadFromJsonAsync&lt;APIResponseData&lt;string&gt;&gt;();
    /// // result.Data will contain "Restarting application..."
    /// </code>
    /// </remarks>
    /// <returns>
    /// <see cref="APIResponseData{T}"/> where T is <see cref="string"/>
    /// containing confirmation message if successful, or error details if failed
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to determine current process filename</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the process cannot be started</exception>
    [Tags(tags: "Apps Management"), HttpPost(template: "RestartApps")]
    public APIResponseData<string> RestartApps()
    {
        try
        {
            using var _ = Process.Start(
                startInfo: new ProcessStartInfo
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    FileName =
                        (Process.GetCurrentProcess().MainModule?.FileName)
                        ?? throw new InvalidOperationException(
                            message: "Cannot get current process filename"
                        ),
                }
            );

            Environment.Exit(exitCode: 0);
            return new APIResponseData<string>().ChangeData(data: "Restarting application...");
        }
        catch (Exception ex)
        {
            return new APIResponseData<string>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Gets list of log files from configured directory
    /// </summary>
    /// <returns>List of log files with their details</returns>
    [Tags(tags: "Apps Management"), HttpGet(template: "Logs/Files")]
    public APIResponseData<List<object>> GetLogFiles()
    {
        try
        {
            var fullPath = SystemLoggingLogic.GetFullLogPath(
                baseDirectory: appConfigs.Get(
                    path: "Logging.baseDirectory",
                    defaultValue: Directory.GetCurrentDirectory()
                ),
                logDirectory: appConfigs.Get(path: "Logging.LogDirectory", defaultValue: "logs")
            );

            if (!Directory.Exists(path: fullPath))
                throw new DirectoryNotFoundException(
                    message: string.Format(
                        format: language.GetMessage(path: "logging.directory_not_found"),
                        arg0: fullPath
                    )
                );

            return new APIResponseData<List<object>>()
                .ChangeStatus(language: language, key: "api.status.success")
                .ChangeData(
                    data:
                    [
                        .. Directory
                            .GetFiles(path: fullPath, searchPattern: "logs-*.txt")
                            .Select(selector: f => new FileInfo(fileName: f))
                            .Select(selector: f =>
                                SystemLoggingLogic.CreateFileInfo(
                                    file: f,
                                    requestScheme: Request.Scheme,
                                    requestHost: Request.Host.Value
                                )
                            )
                            .OrderByDescending(keySelector: f => ((dynamic)f).Modified),
                    ]
                );
        }
        catch (Exception ex)
        {
            return new APIResponseData<List<object>>()
                .ChangeStatus(language: language, key: "api.status.failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    /// Executes a dynamic aggregation request.
    /// </summary>
    /// <remarks>
    /// This endpoint provides a way to execute complex aggregation functions on the server.
    /// The request object should contain the following properties:
    /// - AggName: The name of the aggregation function to be executed.
    /// - Detail: The detail object containing the request data.
    ///
    /// Example aggregation function configuration:
    /// <code>
    /// {
    ///     "sum": {
    ///         "Function": "sum",
    ///         "Operator": "|",
    ///         "Content": "data[].RatingCode"
    ///     },
    ///     "count": {
    ///         "Function": "count",
    ///         "Operator": "|",
    ///         "Content": "data[].RatingCode"
    ///     }
    /// }
    /// </code>
    ///
    /// Example usage:
    /// <code>
    /// var response = await httpClient.PostAsync("api/Demo/DynamicAggregate", new ReqAggregate {
    ///     AggName = "sum",
    ///     Detail = new JObject {
    ///         data = new JArray(
    ///             new JObject {
    ///                 RatingCode = "A"
    ///             },
    ///             new JObject {
    ///                 RatingCode = "B"
    ///             }
    ///         )
    ///     }
    /// });
    /// </code>
    /// </remarks>
    /// <param name="req">The request object containing the aggregation function name and detail.</param>
    /// <returns>
    /// containing the result of the aggregation if successful, or error details if failed
    /// </returns>
    [Tags(tags: "Aggregation"), HttpPost(template: "Dynamic")]
    public APIResponseData<dynamic> DynamicAggregate(ReqAggregate req)
    {
        try
        {
            JObject aggregationsConf =
                dynamicAggrConfigs.Get<JObject>(path: req.AggName)
                ?? throw new DataException("Aggregation config not found");

            JObject request = req.Detail;

            JObject dataResult = [];

            foreach (var agg in aggregationsConf)
            {
                string config = agg.Value?.ToString() ?? string.Empty;

                // --- Step 1: Extract Main Components (Function and Operator) ---

                // Pattern: (Function Name)(Parentheses Content) | Operator
                // (\w+) matches and captures one or more word characters (the function name: sum, count).
                // (\([^)]+\)) matches and captures the entire parenthesized argument: (data[].RatingCode).
                // (|/|) matches the division operator.
                // We use the | (OR) to match either a function call or an operator.
                string mainPattern = @"(\w+)?\([^)]+\)|(/|\*|\+|-|\*)";

                // Get all matches for function calls and operators
                MatchCollection mainMatches = Regex.Matches(config, mainPattern);

                Console.WriteLine("--- Extracted Main Components (Function, Operator) ---");

                // List to hold the extracted components for the next loop
                List<string> components =
                [
                    .. mainMatches.Cast<Match>().Select(selector: m => m.Value),
                ];

                components.ForEach(action: v => Console.WriteLine(v));

                Console.WriteLine("\n--- Extracted Inner Logic (Function Name, Content) ---");

                // --- Step 2: Loop and Extract Inner Logic from Function Components ---

                // Pattern for the inner extraction:
                // (\w+) matches the function name (sum or count).
                // , matches the literal comma.
                // (.*?) matches the content inside the parentheses (non-greedy).
                string innerPattern = @"(\w+)\((.*?)\)";

                List<object?> values = [];

                // Loop through the components captured in Step 1
                foreach (string component in components)
                {
                    // Only process components that are function calls (contain a parenthesis)
                    if (component.Contains('('))
                    {
                        Match innerMatch = Regex.Match(component, innerPattern);

                        if (innerMatch.Success)
                        {
                            // Group 1: Function name (e.g., sum, count)
                            string functionName = innerMatch.Groups[1].Value.ToLower();

                            // Group 2: Content inside parentheses (e.g., data[].RatingCode)
                            string content = innerMatch.Groups[2].Value;

                            // functionName + "(" + "1,2,3,4" + ")" + "/" 4

                            if (functionName == "sum")
                            {
                                string[] fieldsWhere = content.Split('~');
                                string mainContent = fieldsWhere[0];

                                var mainPart = mainContent.ParseJsonPath();

                                if (mainPart.BeforeArrays.Count > 0)
                                {
                                    string arrayPath = mainPart.BeforeArrays[0];
                                    string[] valueField = mainPart.AfterLastArray.Split('.');

                                    JArray? jsonArray = JArray.Parse(
                                        GetFieldValue(request, arrayPath.Split('.')).CastToString()
                                            ?? "[]"
                                    );

                                    jsonArray = CheckWhereCondition(
                                        dataResult,
                                        fieldsWhere,
                                        jsonArray
                                    );

                                    decimal sumValue = jsonArray!
                                        .Children<JObject>()
                                        .Sum(item =>
                                            GetFieldValue(item, valueField).CastToDecimal(0) ?? 0
                                        );
                                    values.Add(sumValue);
                                }
                            }
                            else if (functionName == "count")
                            {
                                string[] contents = content.Split('~');
                                JArray? jsonArray =
                                    JToken
                                        .Parse(request.ToString())
                                        .GetTokenByPath(contents[0].Replace("[]", "")) as JArray;
                                jsonArray = CheckWhereCondition(dataResult, contents, jsonArray);

                                int linqCount = jsonArray?.Count ?? 0;
                                values.Add(linqCount);
                            }
                            else if (functionName == "max")
                            {
                                string[] fieldsWhere = content.Split('~');
                                string mainContent = fieldsWhere[0];

                                var mainPart = mainContent.ParseJsonPath();

                                if (mainPart.BeforeArrays.Count > 0)
                                {
                                    string arrayPath = mainPart.BeforeArrays[0];
                                    string[] valueField = mainPart.AfterLastArray.Split('.');

                                    JArray? jsonArray = JArray.Parse(
                                        GetFieldValue(request, arrayPath.Split('.'))
                                            .CastToString("[]") ?? "[]"
                                    );

                                    jsonArray = CheckWhereCondition(
                                        dataResult,
                                        fieldsWhere,
                                        jsonArray
                                    );

                                    decimal maxValue = jsonArray!
                                        .Children<JObject>()
                                        .Max(item =>
                                            GetFieldValue(item, valueField).CastToDecimal(0) ?? 0
                                        );
                                    values.Add(maxValue);
                                }
                            }
                            else if (functionName == "min")
                            {
                                string[] fieldsWhere = content.Split('~');
                                string mainContent = fieldsWhere[0];

                                var mainPart = mainContent.ParseJsonPath();

                                if (mainPart.BeforeArrays.Count > 0)
                                {
                                    string arrayPath = mainPart.BeforeArrays[0];
                                    string[] valueField = mainPart.AfterLastArray.Split('.');

                                    JArray? jsonArray = JArray.Parse(
                                        GetFieldValue(request, arrayPath.Split('.')).CastToString()
                                            ?? "[]"
                                    );

                                    jsonArray = CheckWhereCondition(
                                        dataResult,
                                        fieldsWhere,
                                        jsonArray
                                    );

                                    string? minValue = jsonArray!
                                        .Children<JObject>()
                                        .Min(item =>
                                            GetFieldValue(item, valueField).CastToString()
                                        );
                                    values.Add(minValue ?? "");
                                }
                            }
                            else if (functionName == "bool")
                            {
                                // Implement bool logic here
                                string[] fieldsWhere = content.Split('~');
                                string[] conditionBool = fieldsWhere[0].Split(' ');
                                string mainContent = conditionBool[0];

                                var mainPart = mainContent.ParseJsonPath();
                                if (mainPart.BeforeArrays.Count > 0)
                                {
                                    string arrayPath = mainPart.BeforeArrays[0];
                                    string[] valueField = mainPart.AfterLastArray.Split('.');
                                    JArray? jsonArray = JArray.Parse(
                                        GetFieldValue(request, arrayPath.Split('.')).CastToString()
                                            ?? "[]"
                                    );

                                    jsonArray = CheckWhereCondition(
                                        dataResult,
                                        fieldsWhere,
                                        jsonArray
                                    );

                                    bool boolValue = jsonArray!
                                        .Children<JObject>()
                                        .Any(item =>
                                        {
                                            string? fieldValue = GetFieldValue(item, valueField)
                                                .CastToString();
                                            string compareValue = conditionBool[1];

                                            return compareValue switch
                                            {
                                                ">" => Convert.ToDecimal(fieldValue)
                                                    > Convert.ToDecimal(conditionBool[2]),
                                                "<" => Convert.ToDecimal(fieldValue)
                                                    < Convert.ToDecimal(conditionBool[2]),
                                                _ => fieldValue == conditionBool[2],
                                            };
                                        });
                                    values.Add(boolValue);
                                }
                            }
                            else if (functionName == "month")
                            {
                                List<string> parts = SplitOutsideCaret(content);

                                var dtStart = GetDateTimeConfig(parts, dataResult, 0);
                                var dtEnd = GetDateTimeConfig(parts, dataResult, 1);

                                int dateValue = GetMonthDifference(dtStart, dtEnd);

                                values.Add(dateValue);
                            }
                            else
                            {
                                throw new DataException($"Unsupported function: {functionName}");
                            }
                        }
                        else if (component.Contains('.'))
                        {
                            string[] fieldsWhere = component[1..^1].Split('~');
                            string mainContent = fieldsWhere[0];

                            var mainPart = mainContent.ParseJsonPath();

                            if (mainPart.BeforeArrays.Count > 0)
                            {
                                string arrayPath = mainPart.BeforeArrays[0];
                                string[] valueField = mainPart.AfterLastArray.Split('.');

                                JArray? jsonArray = JArray.Parse(
                                    GetFieldValue(request, arrayPath.Split('.')).CastToString()
                                        ?? "[]"
                                );

                                int arrIndex = 0;
                                if (valueField[0].Contains('['))
                                {
                                    arrIndex = Convert.ToInt32(
                                        RegexBrackets().Match(valueField[0]).Groups[1].Value
                                    );
                                    valueField = valueField[1..];
                                }

                                jsonArray = CheckWhereCondition(dataResult, fieldsWhere, jsonArray);

                                var resValue = GetFieldValue(
                                    jsonArray?[arrIndex] as JObject ?? [],
                                    valueField
                                );
                                values.Add(resValue);
                            }
                        }
                        else
                        {
                            values.Add(request[component[1..^1]] ?? component);
                        }
                    }
                    // Output operators directly
                    else if (component is "+" or "-" or "*" or "/")
                        values.Add(component);
                }

                // --- Step 3: Perform Final Calculation ---
                var result = EvaluateSimple(values);

                dataResult.Add(agg.Key, JToken.FromObject(result ?? 0));
            }

            return new APIResponseData<dynamic>().ChangeData(data: dataResult);
        }
        catch (Exception ex)
        {
            return new APIResponseData<dynamic>()
                .ChangeStatus(language: language, key: CON_API_STATUS_FAILED)
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    private static JArray? CheckWhereCondition(
        JObject dataResult,
        string[] contents,
        JArray? jsonArray
    )
    {
        if (contents.Length > 1)
        {
            string whereContent = contents[1].Trim();
            jsonArray = SetArrayWhere(dataResult, jsonArray, whereContent);

            string orderContent = string.Empty;
            if (contents.Length > 2)
                orderContent = contents[2];
            jsonArray = SetArrayOrdered(jsonArray, orderContent);
        }

        return jsonArray;
    }

    private static JArray? SetArrayWhere(JObject dataResult, JArray? jsonArray, string whereContent)
    {
        if (string.IsNullOrWhiteSpace(whereContent))
            return jsonArray;

        return new JArray(
            jsonArray
                ?.Children<JObject>()
                .Where(item => EvaluateExpression(whereContent, item, dataResult)) ?? []
        );
    }

    private static bool EvaluateExpression(string expr, JObject row, JObject dataResult)
    {
        // Replace #variables
        expr = Regex.Replace(
            expr,
            @"#(\w+)",
            m =>
            {
                string key = m.Groups[1].Value;
                return dataResult[key]?.ToString() ?? "0";
            }
        );

        // Normalize LIKE / IN tokens
        expr = expr.Replace("NOT LIKE", "NOTLIKE");
        expr = expr.Replace("NOT IN", "NOTIN");

        // Tokenize including math operators
        var tokens = Regex
            .Matches(
                expr,
                @"\(|\)|>=|<=|!=|=|>|<|NOTLIKE|LIKE|NOTIN|IN|\+|\-|\*|\/|&&|\|\||[\w\.]+|'[^']*'"
            )
            .Select(m => m.Value)
            .ToList();

        int pos = 0;

        // Grammar functions
        Func<bool> parseOr = null!;
        Func<bool> parseAnd = null!;
        Func<bool> parseComparison = null!;
        Func<double> parseMath = null!;

        // OR
        parseOr = () =>
        {
            bool left = parseAnd();
            while (pos < tokens.Count && tokens[pos] == "||")
            {
                pos++;
                left = left || parseAnd();
            }
            return left;
        };

        // AND
        parseAnd = () =>
        {
            bool left = parseComparison();
            while (pos < tokens.Count && tokens[pos] == "&&")
            {
                pos++;
                left = left && parseComparison();
            }
            return left;
        };

        // Math: + and -
        parseMath = () =>
        {
            double left = ParseTerm();
            while (pos < tokens.Count && (tokens[pos] == "+" || tokens[pos] == "-"))
            {
                string op = tokens[pos++];
                double right = ParseTerm();
                left = op == "+" ? left + right : left - right;
            }
            return left;
        };

        // Math: * and /
        double ParseTerm()
        {
            double left = ParseFactor();

            while (pos < tokens.Count && (tokens[pos] == "*" || tokens[pos] == "/"))
            {
                string op = tokens[pos++];
                double right = ParseFactor();
                left = op == "*" ? left * right : left / right;
            }
            return left;
        }

        // Number, field, parentheses
        double ParseFactor()
        {
            string t = tokens[pos];

            // (expr)
            if (t == "(")
            {
                pos++;
                double v = parseMath();
                pos++; // skip ')'
                return v;
            }

            pos++;

            // Number
            if (double.TryParse(t, out double n))
                return n;

            // Field -> get row value
            string val = GetFieldValue(row, t.Split('.')).CastToString() ?? "0";
            return double.TryParse(val, out n) ? n : 0;
        }

        // Comparison and special operators
        parseComparison = () =>
        {
            if (tokens[pos] == "(")
            {
                pos++;
                bool val = parseOr();
                pos++; // )
                return val;
            }

            string leftRaw = tokens[pos++];
            string op = tokens[pos++];

            // LIKE / IN detection
            if (op is "LIKE" or "NOTLIKE" or "IN" or "NOTIN")
            {
                string right = tokens[pos++].Trim('\'');

                string leftVal = GetFieldValue(row, leftRaw.Split('.')).CastToString() ?? "";

                return op switch
                {
                    "LIKE" => leftVal.Contains(right, StringComparison.OrdinalIgnoreCase),
                    "NOTLIKE" => !leftVal.Contains(right, StringComparison.OrdinalIgnoreCase),
                    "IN" => right.Split(',').Contains(leftVal),
                    "NOTIN" => !right.Split(',').Contains(leftVal),
                    _ => false,
                };
            }

            // Fixed comparison logic for string & numbers
            string leftValRaw = GetFieldValue(row, leftRaw.Split('.')).CastToString() ?? "";
            string rightToken = tokens[pos++];
            string rightVal = rightToken.Trim('\'');

            bool leftIsNum = double.TryParse(leftValRaw, out double leftNum);
            bool rightIsNum = double.TryParse(rightVal, out double rightNum);

            if (leftIsNum && rightIsNum)
            {
                return op switch
                {
                    "=" => leftNum.Equals(rightNum), // == rightNum,
                    "!=" => !leftNum.Equals(rightNum), // != rightNum,
                    ">" => leftNum > rightNum,
                    "<" => leftNum < rightNum,
                    ">=" => leftNum >= rightNum,
                    "<=" => leftNum <= rightNum,
                    _ => false,
                };
            }

            // String comparison fallback
            return op switch
            {
                "=" => leftValRaw == rightVal,
                "!=" => leftValRaw != rightVal,
                _ => false,
            };
        };

        return parseOr();
    }

    private static JArray? SetArrayOrdered(JArray? jsonArray, string orderContent)
    {
        if (!string.IsNullOrEmpty(orderContent))
        {
            string[] orderParts = orderContent.Split([" "], StringSplitOptions.None);
            if (orderParts.Length > 1)
            {
                string orderFieldFull = orderParts[0].Trim();
                string orderDirection = orderParts[1].Trim();

                string[] orderFieldParts = orderFieldFull.Split('.');

                if (orderDirection == "asc")
                    jsonArray = new JArray(
                        jsonArray!
                            .Children<JObject>()
                            .OrderBy(item => GetFieldValue(item, orderFieldParts).CastToString())
                    );
                else
                    jsonArray = new JArray(
                        jsonArray!
                            .Children<JObject>()
                            .OrderByDescending(item =>
                                GetFieldValue(item, orderFieldParts).CastToString()
                            )
                    );
            }
        }

        return jsonArray;
    }

    private static object? GetFieldValue(JObject item, string[] values)
    {
        JToken? current = item;
        foreach (var part in values)
        {
            current = current?[part];
            if (current == null)
                break;
        }

        object? fieldVal = current?.ToObject<object>();
        return fieldVal;
    }

    //a + b diekstrak jadi 3 token: [a][+][b]
    //a + b * c - d / e diekstrak jadi 3 token: [a][+][b][*][c][-][d][/][e]
    private static object? EvaluateSimple(List<object?> tokens)
    {
        if (tokens.Count == 1)
            return tokens[0];

        if (tokens.Count != 3)
            return 0;

        double left = Convert.ToDouble(tokens[0]);
        string op = tokens[1]?.ToString() ?? throw new ArgumentException("Operator cannot be null");
        double right = Convert.ToDouble(tokens[2]);

        return op switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => left / right,
            _ => throw new InvalidOperationException("Unknown operator"),
        };
    }

    private static int GetMonthDifference(DateTime startDate, DateTime endDate)
    {
        // Ensure endDate is after startDate
        if (startDate > endDate)
        {
            (endDate, startDate) = (startDate, endDate);
        }

        int months = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;

        // Adjust if the day of the month in endDate is earlier than in startDate
        if (endDate.Day < startDate.Day)
        {
            months--;
        }

        return months;
    }

    private static DateTime GetDateTimeConfig(List<string> parts, JObject dataResult, int index)
    {
        var (fieldDateStr, formatDt) = ParseCaretPart(parts[index]);
        return GetDateField(dataResult, fieldDateStr, formatDt);
    }

    private static DateTime GetDateField(JObject dataResult, string fieldDateStr, string formatDt)
    {
        DateTime fieldDate;
        if (fieldDateStr.Equals("NOW", StringComparison.CurrentCultureIgnoreCase))
        {
            fieldDate = DateTime.Now;
        }
        else if (fieldDateStr.Equals("TODAY", StringComparison.CurrentCultureIgnoreCase))
        {
            fieldDate = DateTime.Today;
        }
        else if (fieldDateStr.StartsWith('#'))
        {
            fieldDate = DateTime.ParseExact(
                dataResult.GetValue(fieldDateStr[1..])?.ToString()
                    ?? DateTime.Now.ToString(formatDt),
                formatDt,
                CultureInfo.InvariantCulture
            );
        }
        else
        {
            if (
                !DateTime.TryParseExact(
                    fieldDateStr,
                    formatDt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out fieldDate
                )
            )
            {
                throw new DataException($"Invalid date format: {fieldDateStr}");
            }
        }

        return fieldDate;
    }

    // Fungsi untuk split string di luar ^...^
    private static List<string> SplitOutsideCaret(string input)
    {
        var result = new List<string>();

        // Regex: ^...^ (termasuk spasi di dalam) OR kata tanpa spasi
        var pattern = @"([^^\s]+\^[^^]*\^)|([^\s]+)";
        var matches = Regex.Matches(input, pattern);

        result.AddRange(
            matches
                .Cast<Match>()
                .Select(m => m.Groups[1].Length > 0 ? m.Groups[1].Value : m.Groups[2].Value)
        );

        return result;
    }

    // Fungsi untuk parse Name & Format dari part ^...^
    private static (string Name, string Format) ParseCaretPart(string part)
    {
        // Support ^...^ atau #field^format^
        var pattern = @"^([^^]+)\^([^^]+)\^?$";
        var match = Regex.Match(part, pattern);

        if (match.Success)
            return (match.Groups[1].Value, match.Groups[2].Value);

        return (part, "yyyy-MM-dd hh:mm:ss.fff"); // default format
    }

    [GeneratedRegex(@"\[(.*?)\]")]
    private static partial Regex RegexBrackets();
}
