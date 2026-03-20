using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Hangfire.Common;
using IDC.Utilities.Extensions;
using Newtonsoft.Json.Linq;
using org.matheval;

namespace IDC.AggrMapping.Utilities;

internal partial class JsonQueryEngine
{
    private readonly Dictionary<string, Func<string, string>> _customFuncRegistry = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, object?> _processedResults = [];
    private readonly JObject _sourceData;
    private readonly StringBuilder _sbLog = new();

    [GeneratedRegex(@"([\w#]+)\[\]\.([\w]+)(?:\[(\d+(?:\s*,\s*\d+)*)\])?")]
    private static partial Regex ArrayFieldQueryPatternRegex();

    [GeneratedRegex(@"\bAVG\s*\(", RegexOptions.IgnoreCase, "en-ID")]
    private static partial Regex AvgRegex();

    [GeneratedRegex(@"\bCOUNT\(([^()]+)\)", RegexOptions.IgnoreCase, "en-ID")]
    private static partial Regex CountFunctionRegex();

    // Untuk matching "COUNT(...)" sebagai keseluruhan string
    [GeneratedRegex(@"^COUNT\((.+?)\)$", RegexOptions.IgnoreCase, "en-ID")]
    private static partial Regex FullCountExpressionRegex();

    // [GeneratedRegex(@"([\w]+\[\]|#[\w_]+)")]
    [GeneratedRegex(@"([\w#]+\[\]|#[\w_]+)")]
    private static partial Regex SubQueryPatternRegex();

    /// <summary>
    ///     Konstruktor
    /// </summary>
    /// <param name="jsonContext">
    ///     Data JSON
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Jika <paramref name="jsonContext"/> kosong
    /// </exception>
    internal JsonQueryEngine(JObject jsonContext)
    {
        jsonContext.ThrowIfNull(paramName: nameof(jsonContext));
        _sourceData = jsonContext;
        RegisterCustomFunctions();
    }

    /// <summary>
    ///     Konstruktor
    /// </summary>
    /// <param name="jsonContext">
    ///     Data JSON
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Jika <paramref name="jsonContext"/> kosong
    /// </exception>
    internal JsonQueryEngine(string jsonContext)
    {
        if (string.IsNullOrWhiteSpace(value: jsonContext))
            throw new ArgumentException(
                message: "Source JSON cannot be empty",
                paramName: nameof(jsonContext)
            );

        _sourceData = JObject.Parse(json: jsonContext);
        RegisterCustomFunctions();
    }

    /// <summary>
    ///     Mendaftarkan fungsi-fungsi custom ke <see cref="_customFuncRegistry"/>
    /// </summary>
    private void RegisterCustomFunctions()
    {
        // Handler untuk VAL(query) -> Mengambil nilai tunggal (index 0)
        _customFuncRegistry["val"] = (inner) =>
        {
            var evaluated = EvaluateQuery(queryStr: inner);
            var scalar =
                evaluated is System.Collections.IList list && list.Count > 0 ? list[0] : evaluated;

            return Convert.ToString(value: scalar ?? 0, provider: CultureInfo.InvariantCulture)!;
        };

        // Handler untuk PCT(awal, akhir) -> Konversi ke formula (A-B)/A * 100
        _customFuncRegistry["pct"] = (args) =>
        {
            // Memecah argumen dengan pengamanan (mengabaikan koma di dalam kurung/filter)
            var parts = SplitArgumentsIgnoreBrackets(args: args);

            if (parts.Count < 2)
                return "0";

            // Evaluasi rekursif (mendukung nesting fungsi di dalam PCT)
            var sAwal = Convert.ToString(
                value: ProcessMathOrQuery(queryStr: parts[0]) ?? 0,
                provider: CultureInfo.InvariantCulture
            )!;

            // Handle pembagian dengan nol secara sederhana
            if (sAwal == "0")
                return "0";

            var sAkhir = Convert.ToString(
                value: ProcessMathOrQuery(queryStr: parts[1]) ?? 0,
                provider: CultureInfo.InvariantCulture
            )!;

            return $"(({sAwal} - {sAkhir}) / {sAwal}) * 100";
        };
    }

    public JObject AggregateProcessor(
        JObject? queryConfig,
        Action<JObject, StringBuilder>? callback = default
    )
    {
        if (queryConfig == null)
            return [];

        JObject finalResult = [];

        foreach (var property in queryConfig.Properties())
        {
            var queryKey = property.Name;
            var queryString = property.Value?.ToString();

            if (string.IsNullOrEmpty(value: queryString))
            {
                finalResult.Add(propertyName: queryKey, value: JValue.CreateNull());
                continue;
            }

            object? result = ProcessMathOrQuery(queryStr: queryString);
            _processedResults[queryKey] = result;

            finalResult.Add(
                queryKey,
                result != null ? JToken.FromObject(o: result) : JValue.CreateNull()
            );
        }

        callback?.Invoke(finalResult, _sbLog);

        return finalResult;
    }

    private static object ComputeExpression(
        string templateExpr,
        Dictionary<string, object?> bindings
    )
    {
        Expression expr = new(formular: templateExpr);
        foreach (var bind in bindings)
            expr.Bind(key: bind.Key, value: bind.Value);

        return expr.Eval();
    }

    private static DataTable CreateFullDataTable(JArray array)
    {
        DataTable dt = new();
        if (array.Count == 0)
            return dt;

        var properties = array
            .Children<JObject>()
            .SelectMany(selector: o => o.Properties())
            .Select(selector: p => p.Name)
            .Distinct();

        foreach (var propName in properties)
        {
            dt.Columns.Add(
                columnName: propName,
                type: array
                    .Children<JObject>()
                    .Select(selector: o => o[propName])
                    .FirstOrDefault(predicate: v => v != null && v.Type != JTokenType.Null)
                    ?.Type switch
                {
                    JTokenType.Integer or JTokenType.Float => typeof(double),
                    JTokenType.Boolean => typeof(bool),
                    JTokenType.Date => typeof(DateTime),
                    _ => typeof(string),
                }
            );
        }

        foreach (JObject obj in array.Children<JObject>())
        {
            DataRow row = dt.NewRow();

            foreach (DataColumn col in dt.Columns)
            {
                var val = obj[col.ColumnName];

                row[col.ColumnName] =
                    (val == null || val.Type == JTokenType.Null)
                        ? DBNull.Value
                        : val.ToObject(objectType: col.DataType);
            }

            dt.Rows.Add(row: row);
        }

        return dt;
    }

    private static string EvaluateCountCalls(string templateExpr)
    {
        templateExpr = CountFunctionRegex()
            .Replace(
                input: templateExpr,
                evaluator: static m =>
                    m.Groups[1]
                        .Value.Split(separator: ',')
                        .Count(predicate: static s => !string.IsNullOrWhiteSpace(value: s))
                        .ToString()
            );
        return templateExpr;
    }

    private static void ExtractSubQueries(string templateExpr, List<string> subQueries)
    {
        foreach (Match m in SubQueryPatternRegex().Matches(input: templateExpr))
        {
            if (m.Value.StartsWith('#'))
            {
                if (!subQueries.Contains(m.Value))
                    subQueries.Add(m.Value);
                continue;
            }

            int startIndex = m.Index;
            int currentIndex = startIndex + m.Length;
            int openParenCount = 0;
            bool hasReachedTilde = false;

            while (currentIndex < templateExpr.Length)
            {
                (bool flowControl, (openParenCount, hasReachedTilde)) =
                    HandleCharInFilterExpression(
                        templateExpr,
                        currentIndex,
                        openParenCount,
                        hasReachedTilde
                    );
                if (!flowControl)
                {
                    break;
                }

                currentIndex++;
            }

            string subQuery = templateExpr[startIndex..currentIndex].Trim();
            if (!subQueries.Contains(subQuery))
                subQueries.Add(subQuery);
        }

        static (
            bool flowControl,
            (int openParenCount, bool hasReachedTilde) value
        ) HandleCharInFilterExpression(
            string templateExpr,
            int currentIndex,
            int openParenCount,
            bool hasReachedTilde
        )
        {
            char c = templateExpr[currentIndex];

            if (c == '(')
                openParenCount++;
            if (c == ')')
            {
                if (openParenCount <= 0)
                    // Return flowControl false to stop scanning
                    return (flowControl: false, value: (openParenCount, hasReachedTilde));
                openParenCount--;
            }

            if (c == '~')
                hasReachedTilde = true;

            if (!hasReachedTilde && openParenCount == 0 && "+-*/%^&, ".Contains(c))
                return (flowControl: false, value: (openParenCount, hasReachedTilde));

            if (hasReachedTilde && openParenCount == 0 && "+-*/%^&,".Contains(c))
            {
                return (flowControl: false, value: (openParenCount, hasReachedTilde));
            }

            // FIX: Pass the updated state back to the loop
            return (flowControl: true, value: (openParenCount, hasReachedTilde));
        }
    }

    private static int FindClosingBracket(string text, int openPos)
    {
        var closePos = openPos;
        var counter = 1;

        while (counter > 0 && closePos < text.Length)
        {
            var c = text[closePos++];

            if (c == '(')
                counter++;
            else if (c == ')')
                counter--;
        }

        return counter == 0 ? closePos - 1 : -1;
    }

    private static string PrepareSqlFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(value: filter))
            return "";

        foreach (
            var (oldValue, newValue) in new Dictionary<string, string>
            {
                { "&&", " AND " },
                { "||", " OR " },
                { "==", "=" },
                { "!=", "<>" },
                { "=>", ">=" },
                { "=<", "<=" },
            }
        )
        {
            filter = filter.Replace(
                oldValue: oldValue,
                newValue: newValue,
                comparisonType: StringComparison.Ordinal
            );
        }

        return filter;
    }

    private static List<string> SplitArgumentsIgnoreBrackets(string args)
    {
        var result = new List<string>();
        var bracketCount = 0;
        var lastIndex = 0;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == '(')
                bracketCount++;
            else if (args[i] == ')')
                bracketCount--;
            else if (args[i] == ',' && bracketCount == 0)
            {
                result.Add(item: args[lastIndex..i].Trim());
                lastIndex = i + 1;
            }
        }

        result.Add(item: args[lastIndex..].Trim());
        return result;
    }

    private string ApplyCustomFunctions(string templateExpr)
    {
        foreach (var func in _customFuncRegistry)
        {
            var pattern = $@"\b{func.Key}\s*\(";
            while (
                Regex.IsMatch(
                    input: templateExpr,
                    pattern: pattern,
                    options: RegexOptions.IgnoreCase
                )
            )
            {
                var match = Regex.Match(
                    input: templateExpr,
                    pattern: pattern,
                    options: RegexOptions.IgnoreCase
                );
                var startIdx = match.Index + match.Length;
                var endIdx = FindClosingBracket(text: templateExpr, openPos: startIdx);

                if (endIdx == -1)
                    break;

                templateExpr = templateExpr
                    .Remove(startIndex: match.Index, count: endIdx + 1 - match.Index)
                    .Insert(
                        startIndex: match.Index,
                        value: func.Value(templateExpr[startIdx..endIdx])
                    );
            }
        }

        return templateExpr;
    }

    private void BindAndReplaceSubQueries(
        ref string templateExpr,
        Dictionary<string, object?> bindings,
        ref int aliasCount,
        List<string> subQueries
    )
    {
        foreach (var subQuery in subQueries.OrderByDescending(keySelector: static s => s.Length))
        {
            var val = EvaluateQuery(queryStr: subQuery);
            if (val is System.Collections.IList list)
            {
                var listAliases = new List<string>();
                foreach (var item in list)
                {
                    var itemAlias = $"val_{aliasCount++}";
                    listAliases.Add(item: itemAlias);
                    bindings.Add(key: itemAlias, value: item);
                }
                templateExpr = templateExpr.Replace(
                    oldValue: subQuery,
                    newValue: listAliases.Count > 0
                        ? string.Join(separator: ", ", values: listAliases)
                        : "0"
                );
            }
            else
            {
                var safeAlias = $"var_{aliasCount++}";
                templateExpr = templateExpr.Replace(oldValue: subQuery, newValue: safeAlias);
                bindings.Add(key: safeAlias, value: val);
            }
        }
    }

    private object? EvaluateQuery(string queryStr)
    {
        static object? ExtractItemsByIndices(string indexCsv, List<object?> projected)
        {
            var indexedResults = new List<object?>();
            var indices = indexCsv
                .Split(separator: ',')
                .Select(selector: s => int.Parse(s: s.Trim()))
                .ToList();

            foreach (var i in indices)
                if (i >= 0 && i < projected.Count)
                    indexedResults.Add(item: projected[i]);

            if (indexedResults.Count == 0)
                return null;

            return indices.Count > 1 ? indexedResults : indexedResults.FirstOrDefault();
        }

        // 1. Handling Lookup Hashtag (#)
        if (queryStr.StartsWith(value: '#'))
        {
            var key = queryStr[1..];
            if (_processedResults.TryGetValue(key: key, value: out var val))
                return val;

            // Log if hashtag key is not found
            LogWritter($"Lookup field '#{key}' not found in processed results.");
            return null;
        }

        var parts = queryStr.Split(separator: '~');
        var pathPart = parts[0];
        var filterPart = parts.Length > 1 ? parts[1] : null;
        var sortPart = parts.Length > 2 ? parts[2] : null;

        var match = ArrayFieldQueryPatternRegex().Match(input: pathPart);
        if (!match.Success)
        {
            LogWritter($"Invalid query format or path not recognized: '{queryStr}'.");
            return null;
        }

        var arrayName = match.Groups[1].Value;
        var fieldName = match.Groups[2].Value;
        var indexCsv = match.Groups[3].Value;

        // 2. Handling Missing Array
        if (_sourceData[arrayName] is not JArray targetArray)
        {
            LogWritter($"Array '{arrayName}' not found in source JSON for query: '{queryStr}'.");
            return null;
        }

        using var dt = CreateFullDataTable(array: targetArray);
        string sqlFilter = PrepareSqlFilter(filter: filterPart);
        string sqlSort = (sortPart ?? "").Replace(oldValue: "==", newValue: "=");

        try
        {
            var rows = dt.Select(filterExpression: sqlFilter, sort: sqlSort);
            var projected = rows.Select(selector: r =>
                    r[fieldName] == DBNull.Value ? null : r[fieldName]
                )
                .ToList();

            // 3. Handling Specific Indices Lookup
            if (!string.IsNullOrEmpty(value: indexCsv))
            {
                var result = ExtractItemsByIndices(indexCsv: indexCsv, projected: projected);
                if (result == null)
                {
                    LogWritter(
                        $"Indices [{indexCsv}] out of range or no data for field '{fieldName}' in query: '{queryStr}'."
                    );
                }
                return result;
            }

            // 4. Handling Empty Filter Result
            if (projected.Count == 0)
            {
                LogWritter($"No data matches the filter/criteria for query: '{queryStr}'.");
                return null;
            }

            return projected.Count == 1 ? projected[0] : projected;
        }
        catch (Exception ex)
        {
            // 5. Handling SQL or Processing Error
            LogWritter($"Error during data evaluation for '{queryStr}': {ex.Message}");
            return null;
        }
    }

    private object? ProcessMathOrQuery(string queryStr)
    {
        // Normalisasi nama fungsi karna mathEval tidak memiliki fungsi
        // dengan nama AVG, yang ada adalah AVERAGE
        var templateExpr = AvgRegex().Replace(input: queryStr, replacement: "AVERAGE(");

        // Eksekusi Custom Functions (VAL, PCT, dll)
        templateExpr = ApplyCustomFunctions(templateExpr: templateExpr);

        // Handling Khusus COUNT tunggal di level root
        // disini perlu handling khusus karna mathEval tidak memiliki fungsi COUNT
        (bool flowControl, object? value) = TryHandleRootCount(templateExpr: templateExpr);
        if (!flowControl)
            return value;

        try
        {
            var bindings = new Dictionary<string, object?>();
            var aliasCount = 0;

            // Deteksi Sub-Query (Array[] atau Lookup #)
            var subQueries = new List<string>();
            ExtractSubQueries(templateExpr: templateExpr, subQueries: subQueries);

            // Evaluasi & Binding ke MathEval
            BindAndReplaceSubQueries(
                templateExpr: ref templateExpr,
                bindings: bindings,
                aliasCount: ref aliasCount,
                subQueries: subQueries
            );

            // Handling Khusus COUNT tunggal di level inner
            // disini perlu handling khusus karna mathEval tidak memiliki fungsi COUNT
            templateExpr = EvaluateCountCalls(templateExpr: templateExpr);

            // Eksekusi Akhir dengan org.matheval
            return ComputeExpression(templateExpr: templateExpr, bindings: bindings);
        }
        catch
        {
            // Fallback jika bukan ekspresi matematika
            return EvaluateQuery(queryStr: queryStr);
        }
    }

    private (bool flowControl, object? value) TryHandleRootCount(string templateExpr)
    {
        var countMatch = FullCountExpressionRegex().Match(input: templateExpr);
        if (
            countMatch.Success
            && !templateExpr.Contains(value: '+')
            && !templateExpr.Contains(value: '-')
            && !templateExpr.Contains(value: '*')
        )
        {
            var val = EvaluateQuery(queryStr: countMatch.Groups[1].Value);

            if (val is System.Collections.IList list)
                return (flowControl: false, value: (double)list.Count);

            return (flowControl: false, value: val != null ? 1.0 : 0.0);
        }

        return (flowControl: true, value: null);
    }

    private void LogWritter(string text)
    {
        Console.WriteLine($"[JsonQueryEngine Log] {text}");
        _sbLog.AppendLine(text);
    }
}
