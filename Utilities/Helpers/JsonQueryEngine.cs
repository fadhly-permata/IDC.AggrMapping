using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using Newtonsoft.Json.Linq;
using org.matheval;

namespace IDC.AggrMapping.Utilities.Helpers;

internal partial class JsonQueryEngine
{
    private readonly Dictionary<string, Func<string, string>> _customFuncRegistry = new(
        comparer: StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, object?> _processedResults = [];
    private readonly JObject _sourceData;
    private readonly StringBuilder _sbLog = new();

    [GeneratedRegex(pattern: @"([\w#]+)\[\]\.([\w]+)(?:\[(\d+(?:\s*,\s*\d+)*)\])?")]
    private static partial Regex ArrayFieldQueryPatternRegex();

    [GeneratedRegex(pattern: @"\bAVG\s*\(", options: RegexOptions.IgnoreCase, cultureName: "en-ID")]
    private static partial Regex AvgRegex();

    [GeneratedRegex(
        pattern: @"\bCOUNT\(([^()]+)\)",
        options: RegexOptions.IgnoreCase,
        cultureName: "en-ID"
    )]
    private static partial Regex CountFunctionRegex();

    // Untuk matching "COUNT(...)" sebagai keseluruhan string
    [GeneratedRegex(
        pattern: @"^COUNT\((.+?)\)$",
        options: RegexOptions.IgnoreCase,
        cultureName: "en-ID"
    )]
    private static partial Regex FullCountExpressionRegex();

    // [GeneratedRegex(@"([\w]+\[\]|#[\w_]+)")]
    [GeneratedRegex(pattern: @"([\w#]+\[\]|#[\w_]+)")]
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
        _customFuncRegistry[key: "val"] = string (inner) =>
        {
            var evaluated = EvaluateQuery(queryStr: inner);
            var scalar = evaluated is System.Collections.IList { Count: > 0 } list
                ? list[index: 0]
                : evaluated;

            return Convert.ToString(value: scalar ?? 0, provider: CultureInfo.InvariantCulture)!;
        };

        // Handler untuk PCT(awal, akhir) -> Konversi ke formula (A-B)/A * 100
        _customFuncRegistry[key: "pct"] = string (args) =>
        {
            // Memecah argumen dengan pengamanan (mengabaikan koma di dalam kurung/filter)
            var parts = SplitArgumentsIgnoreBrackets(args: args);

            if (parts.Count < 2)
                return "0";

            // Evaluasi rekursif (mendukung nesting fungsi di dalam PCT)
            var sAwal = Convert.ToString(
                value: ProcessMathOrQuery(queryStr: parts[index: 0]) ?? 0,
                provider: CultureInfo.InvariantCulture
            )!;

            // Handle pembagian dengan nol secara sederhana
            if (sAwal == "0")
                return "0";

            var sAkhir = Convert.ToString(
                value: ProcessMathOrQuery(queryStr: parts[index: 1]) ?? 0,
                provider: CultureInfo.InvariantCulture
            )!;

            return $"(({sAwal} - {sAkhir}) / {sAwal}) * 100";
        };
    }

    internal JObject AggregateProcessorAsync(JObject? queryConfig)
    {
        if (queryConfig == null)
            return [];

        JObject finalResult = [];

        // Kita ubah menjadi sekuensial (foreach biasa tanpa Task.Run massal)
        // agar '#hashtag' bisa membaca nilai yang sudah diproses sebelumnya.
        foreach (var property in queryConfig.Properties())
        {
            var queryKey = property.Name;
            var queryString = property.Value.ToString();

            if (string.IsNullOrEmpty(value: queryString))
            {
                finalResult.Add(propertyName: queryKey, value: JValue.CreateNull());
                continue;
            }

            try
            {
                // Eksekusi langsung secara sekuensial
                var result = ProcessMathOrQuery(queryStr: queryString);

                // Simpan ke dictionary internal agar baris berikutnya bisa melakukan lookup '#'
                _processedResults[key: queryKey] = result;

                // Tambahkan ke hasil akhir
                finalResult.Add(
                    propertyName: queryKey,
                    value: result != null ? JToken.FromObject(o: result) : JValue.CreateNull()
                );
            }
            catch (Exception ex)
            {
                LogWritter(text: $"Error processing key '{queryKey}': {ex.Message}");
                finalResult.Add(propertyName: queryKey, value: JValue.CreateNull());
            }
        }

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
            .SelectMany(selector: IEnumerable<JProperty> (o) => o.Properties())
            .Select(selector: string (p) => p.Name)
            .Distinct();

        foreach (var propName in properties)
        {
            dt.Columns.Add(
                columnName: propName,
                type: array
                    .Children<JObject>()
                    .Select(selector: JToken? (o) => o[propertyName: propName])
                    .FirstOrDefault(predicate: bool (v) => v != null && v.Type != JTokenType.Null)
                    ?.Type switch
                {
                    JTokenType.Integer or JTokenType.Float => typeof(double),
                    JTokenType.Boolean => typeof(bool),
                    JTokenType.Date => typeof(DateTime),
                    _ => typeof(string),
                }
            );
        }

        foreach (var obj in array.Children<JObject>())
        {
            var row = dt.NewRow();

            foreach (DataColumn col in dt.Columns)
            {
                var val = obj[propertyName: col.ColumnName];

                row[columnName: col.ColumnName] =
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
                evaluator: static string (m) =>
                    m.Groups[groupnum: 1]
                        .Value.Split(separator: ',')
                        .Count(predicate: static bool (s) => !string.IsNullOrWhiteSpace(value: s))
                        .ToString()
            );
        return templateExpr;
    }

    private static void ExtractSubQueries(string templateExpr, List<string> subQueries)
    {
        foreach (Match m in SubQueryPatternRegex().Matches(input: templateExpr))
        {
            if (m.Value.StartsWith(value: '#'))
            {
                if (!subQueries.Contains(item: m.Value))
                    subQueries.Add(item: m.Value);
                continue;
            }

            var startIndex = m.Index;
            var currentIndex = startIndex + m.Length;

            // Initialization of scanning states
            var openParenCount = 0;
            var hasReachedTilde = false;
            var inQuote = false; // New state to track if we are inside a string literal

            while (currentIndex < templateExpr.Length)
            {
                // Process the current character and update states
                var result = HandleCharInFilterExpression(
                    templateExpr: templateExpr,
                    currentIndex: currentIndex,
                    openParenCount: openParenCount,
                    hasReachedTilde: hasReachedTilde,
                    inQuote: inQuote
                );

                if (!result.flowControl)
                    break;

                // Update local states for the next iteration
                openParenCount = result.value.openParenCount;
                hasReachedTilde = result.value.hasReachedTilde;
                inQuote = result.value.inQuote;

                currentIndex++;
            }

            var subQuery = templateExpr[startIndex..currentIndex].Trim();
            if (!subQueries.Contains(item: subQuery))
                subQueries.Add(item: subQuery);
        }

        return;

        // Helper function to determine scanning flow and state updates
        static (
            bool flowControl,
            (int openParenCount, bool hasReachedTilde, bool inQuote) value
        ) HandleCharInFilterExpression(
            string templateExpr,
            int currentIndex,
            int openParenCount,
            bool hasReachedTilde,
            bool inQuote
        )
        {
            var c = templateExpr[index: currentIndex];

            // Toggle quote state when a single quote is encountered
            if (c == '\'')
            {
                inQuote = !inQuote;
                return (flowControl: true, value: (openParenCount, hasReachedTilde, inQuote));
            }

            // If we are inside a quote, ignore all breaking logic and continue scanning
            if (inQuote)
                return (flowControl: true, value: (openParenCount, hasReachedTilde, inQuote));

            switch (c)
            {
                // Parentheses tracking for nested expressions
                case '(':
                    openParenCount++;
                    break;
                case ')' when openParenCount <= 0:
                    return (flowControl: false, value: (openParenCount, hasReachedTilde, inQuote));
                case ')':
                    openParenCount--;
                    break;
                // Tilde marker for the start of filter/sort section
                case '~':
                    hasReachedTilde = true;
                    break;
            }

            return hasReachedTilde switch
            {
                // Breaking logic: stop if we hit a delimiter while not inside a quote/parentheses
                false when openParenCount == 0 && "+-*/%^&, ".Contains(value: c) => (
                    flowControl: false,
                    value: (openParenCount, hasReachedTilde, inQuote)
                ),
                true when openParenCount == 0 && "+-*/%^&,".Contains(value: c) => (
                    flowControl: false,
                    value: (openParenCount, hasReachedTilde, inQuote)
                ),
                _ => (flowControl: true, value: (openParenCount, hasReachedTilde, inQuote)),
            };
        }
    }

    private static int FindClosingBracket(string text, int openPos)
    {
        var closePos = openPos;
        var counter = 1;

        while (counter > 0 && closePos < text.Length)
            switch (text[index: closePos++])
            {
                case '(':
                    counter++;
                    break;
                case ')':
                    counter--;
                    break;
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
            filter = filter.Replace(
                oldValue: oldValue,
                newValue: newValue,
                comparisonType: StringComparison.Ordinal
            );

        return filter;
    }

    private static List<string> SplitArgumentsIgnoreBrackets(string args)
    {
        var result = new List<string>();
        var bracketCount = 0;
        var lastIndex = 0;

        for (var i = 0; i < args.Length; i++)
            switch (args[index: i])
            {
                case '(':
                    bracketCount++;
                    break;
                case ')':
                    bracketCount--;
                    break;
                case ',' when bracketCount == 0:
                    result.Add(item: args[lastIndex..i].Trim());
                    lastIndex = i + 1;
                    break;
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
                        value: func.Value(arg: templateExpr[startIdx..endIdx])
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
        foreach (
            var subQuery in subQueries.OrderByDescending(keySelector: static int (s) => s.Length)
        )
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
        // 1. Handling Lookup Hashtag (#)
        if (queryStr.StartsWith(value: '#'))
        {
            var key = queryStr[1..];
            if (_processedResults.TryGetValue(key: key, value: out var val))
                return val;

            // Log if hashtag key is not found
            LogWritter(text: $"Lookup field '#{key}' not found in processed results.");
            return null;
        }

        var parts = queryStr.Split(separator: '~');
        var pathPart = parts[0];
        var filterPart = parts.Length > 1 ? parts[1] : null;
        var sortPart = parts.Length > 2 ? parts[2] : null;

        var match = ArrayFieldQueryPatternRegex().Match(input: pathPart);
        if (!match.Success)
        {
            LogWritter(text: $"Invalid query format or path not recognized: '{queryStr}'.");
            return null;
        }

        var arrayName = match.Groups[groupnum: 1].Value;
        var fieldName = match.Groups[groupnum: 2].Value;
        var indexCsv = match.Groups[groupnum: 3].Value;

        // 2. Handling Missing Array
        if (_sourceData[propertyName: arrayName] is JArray targetArray)
            return Projected(
                queryStr: queryStr,
                targetArray: targetArray,
                filterPart: filterPart,
                sortPart: sortPart,
                fieldName: fieldName,
                s: indexCsv
            );

        LogWritter(text: $"Array '{arrayName}' not found in source JSON for query: '{queryStr}'.");
        return null;
    }

    private object? Projected(
        string queryStr,
        JArray targetArray,
        string? filterPart,
        string? sortPart,
        string fieldName,
        string s
    )
    {
        using var dt = CreateFullDataTable(array: targetArray);
        var sqlFilter = PrepareSqlFilter(filter: filterPart);
        var sqlSort = (sortPart ?? "").Replace(oldValue: "==", newValue: "=");

        try
        {
            var rows = dt.Select(filterExpression: sqlFilter, sort: sqlSort);
            var projected = rows.Select(
                    selector: object? (r) =>
                        r[columnName: fieldName] == DBNull.Value ? null : r[columnName: fieldName]
                )
                .ToList();

            // 3. Handling Specific Indices Lookup
            if (!string.IsNullOrEmpty(value: s))
            {
                var result = ExtractItemsByIndices(indexCsv: s, projected: projected);
                if (result == null)
                    LogWritter(
                        text: $"Indices [{s}] out of range or no data for field '{fieldName}' in query: '{queryStr}'."
                    );

                return result;
            }

            // 4. Handling Empty Filter Result
            if (projected.Count != 0)
                return projected.Count == 1 ? projected[index: 0] : projected;

            LogWritter(text: $"No data matches the filter/criteria for query: '{queryStr}'.");
            return null;
        }
        catch (Exception ex)
        {
            // 5. Handling SQL or Processing Error
            LogWritter(text: $"Error during data evaluation for '{queryStr}': {ex.Message}");
            return null;
        }

        static object? ExtractItemsByIndices(string indexCsv, List<object?> projected)
        {
            var indices = indexCsv
                .Split(separator: ',')
                .Select(selector: int (s) => int.Parse(s: s.Trim()))
                .ToList();

            var indexedResults = (
                from i in indices
                where i >= 0 && i < projected.Count
                select projected[index: i]
            ).ToList();

            if (indexedResults.Count == 0)
                return null;

            return indices.Count > 1 ? indexedResults : indexedResults.FirstOrDefault();
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
        var (flowControl, value) = TryHandleRootCount(templateExpr: templateExpr);
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
            !countMatch.Success
            || templateExpr.Contains(value: '+')
            || templateExpr.Contains(value: '-')
            || templateExpr.Contains(value: '*')
        )
            return (flowControl: true, value: null);

        var val = EvaluateQuery(queryStr: countMatch.Groups[groupnum: 1].Value);
        if (val is System.Collections.IList list)
            return (flowControl: false, value: (double)list.Count);

        return (flowControl: false, value: val != null ? 1.0 : 0.0);
    }

    private void LogWritter(string text)
    {
        Console.WriteLine(value: $"[JsonQueryEngine Log] {text}");
        _sbLog.AppendLine(value: text);
    }

    internal async Task SaveLog(
        LogDataModel logData,
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        // Kalo gak ada error dari proses external, maka gunakan log internal
        if (string.IsNullOrEmpty(value: logData.Log))
        {
            var log = _sbLog.ToString();
            logData.Log = !string.IsNullOrWhiteSpace(value: log)
                ? log
                : "Process completed successfully.";
        }
        await logData.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);
    }
}
