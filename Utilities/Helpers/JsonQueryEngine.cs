using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using Newtonsoft.Json.Linq;
using org.matheval;

namespace IDC.AggrMapping.Utilities.Helpers;

// TODO :
// Berikut adalah daftar optimasi yang bisa dilakukan untuk meningkatkan performa JsonQueryEngine, terutama untuk penggunaan concurrent tinggi (hingga 1000 concurrent):
// 1. [DONE] **Memory Management Enhancement**
//    - Ganti `StringBuilder` dengan `ArrayPool<char>` untuk operasi string yang intensif
//    - Gunakan `ArrayPool<JObject>` lebih agresif untuk temporary array
//    - Implementasi `Dispose` pattern untuk memastikan resources dibersihkan dengan benar
// 2. [DONE] **Regex Optimization**
//    - Cache compiled regex instances sebagai static readonly
//    - Gunakan `RegexGenerator` attribute untuk regex yang kompleks
//    - Hindari penggunaan regex di hot path jika memungkinkan
// 3. [DONE] **Parallel Processing**
//    - Implementasi parallel processing untuk operasi yang bisa diparalelkan (e.g., CreateFlattenedDataTable)
//    - Gunakan `Parallel.ForEach` untuk operasi pada array besar
//    - Implementasi lock-free patterns untuk shared resources
// 4. [Done] **Data Structure Optimization**
//    - Ganti `DataTable` dengan custom collection untuk menghindari overhead
//    - Gunakan `Dictionary<string, object>` sebagai alternatif yang lebih ringan
//    - Implementasi pooling untuk objek yang sering dibuat/dihancurkan
// 5. **String Manipulation**
//    - Kurangi alokasi string dengan `string.Create` dan `Span<T>`
//    - Gunakan `StringComparison.Ordinal` untuk operasi string comparison
//    - Implementasi string interning untuk string yang sering digunakan
// 6. **JObject/JToken Handling**
//    - Gunakan `JToken.SelectToken` dengan path yang di-cache
//    - Hindari deep copy JObject/JToken jika memungkinkan
//    - Gunakan streaming JSON untuk file besar
// 7. **Concurrency Optimization**
//    - Implementasi `ConcurrentDictionary` untuk _customFuncRegistry
//    - Gunakan `ThreadLocal<T>` untuk state yang thread-specific
//    - Hindari lock contention dengan teknik lock striping
// 8. **Logging Optimization**
//    - Implementasi async logging dengan channel atau buffer
//    - Gunakan structured logging untuk mengurangi parsing overhead
//    - Batasi jumlah log di production
// 9. **Query Processing**
//    - Cache hasil evaluasi query yang sering digunakan
//    - Pre-compile expression trees untuk filter yang kompleks
//    - Optimasi algoritma binding sub-queries
// 10. **Memory Pressure Reduction**
//     - Gunakan ValueTask untuk operasi async yang sering completed sync
//     - Implementasi object pooling untuk objek yang berat
//     - Kurangi boxing/unboxing operations
// 11. **Error Handling**
//     - Hindari exception throwing di hot path
//     - Gunakan TryXXX pattern untuk operasi yang mungkin gagal
//     - Batasi stack trace capturing untuk error yang sering terjadi
// 12. **Garbage Collection Optimization**
//     - Gunakan struct untuk data kecil yang sering dialokasikan
//     - Implementasi IDisposable untuk mengontrol GC pressure
//     - Hindari finalizers jika memungkinkan
// 13. **Configuration**
//     - Tambahkan konfigurasi untuk menonaktifkan fitur yang tidak diperlukan
//     - Implementasi adaptive performance tuning
//     - Tambahkan metrics untuk monitoring performa
// 14. **Dependency Optimization**
//     - Evaluasi penggunaan org.matheval untuk kemungkinan replacement
//     - Minimalkan dependency eksternal
//     - Gunakan source generators untuk mengurangi reflection
// 15. **Testing & Profiling**
//     - Tambahkan benchmark untuk operasi kritis
//     - Lakukan memory profiling secara berkala
//     - Implementasi stress testing untuk skenario concurrent

internal partial class JsonQueryEngine : IDisposable
{
    private readonly Dictionary<string, Func<string, string>> _customFuncRegistry = new(
        comparer: StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, object?> _processedResults = [];
    private readonly JObject _sourceData;
    private readonly StringBuilder _sbLog = new();
    private readonly ArrayPool<char> _arrayPool;
    private char[] _logBuffer;
    private int _logPosition;
    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(obj: this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Kembalikan buffer ke pool
            _arrayPool.Return(array: _logBuffer);
            _logBuffer = null!;

            _sbLog.Clear();
        }

        _disposed = true;
    }

    private sealed class LightweightRow
    {
        private readonly Dictionary<string, object?> _data = new(
            comparer: StringComparer.OrdinalIgnoreCase
        );

        public object? this[string column]
        {
            get => _data.TryGetValue(key: column, value: out var value) ? value : null;
            set => _data[key: column] = value;
        }
    }

    // Buat class untuk lightweight data table
    private sealed class LightweightTable : IDisposable
    {
        private bool _disposed;

        public List<string> Columns { get; } = new();
        public List<LightweightRow> Rows { get; } = new();

        public void Dispose()
        {
            if (_disposed)
                return;

            // Clean up managed resources
            Columns.Clear();
            Rows.Clear();

            _disposed = true;
        }
    }
}

internal partial class JsonQueryEngine
{
    [GeneratedRegex(
        pattern: @"([\w#]+)\[\]\.([\w]+)(?:\[(\d+(?:\s*,\s*\d+)*)\])?",
        options: RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex ArrayFieldQueryPatternRegex();

    [GeneratedRegex(
        pattern: @"\bAVG\s*\(",
        options: RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex AvgRegex();

    [GeneratedRegex(
        pattern: @"\bCOUNT\(([^()]+)\)",
        options: RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex CountFunctionRegex();

    [GeneratedRegex(
        pattern: @"^COUNT\((.+?)\)$",
        options: RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex FullCountExpressionRegex();

    [GeneratedRegex(
        pattern: @"([\w#]+\[\]|#[\w_]+)",
        options: RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex SubQueryPatternRegex();

    [GeneratedRegex(
        pattern: @"^(sum|avg|min|max|count)\s*\((.+)\)$",
        options: RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex AggregateFunctionPattern();

    // Caching semua regex calls
    private static readonly Regex ArrayFieldQueryPattern = ArrayFieldQueryPatternRegex();
    private static readonly Regex AvgRegexInstance = AvgRegex();
    private static readonly Regex CountFunctionRegexInstance = CountFunctionRegex();
    private static readonly Regex FullCountExpressionRegexInstance = FullCountExpressionRegex();
    private static readonly Regex SubQueryPatternRegexInstance = SubQueryPatternRegex();
    private static readonly Regex AggregateFunctionPatternInstance = AggregateFunctionPattern();
}

internal partial class JsonQueryEngine
{
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

        _arrayPool = ArrayPool<char>.Shared;
        _logBuffer = _arrayPool.Rent(minimumLength: 4096);
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

        _arrayPool = ArrayPool<char>.Shared;
        _logBuffer = _arrayPool.Rent(minimumLength: 4096);
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
                var result = ProcessMathOrQuery(queryStr: queryString);
                _processedResults[key: queryKey] = result;
                finalResult.Add(
                    propertyName: queryKey,
                    value: result != null ? JToken.FromObject(o: result) : JValue.CreateNull()
                );
            }
            catch (Exception ex)
            {
                LogWriter(text: $"Error processing key '{queryKey}': {ex.Message}");
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

    private static string EvaluateCountCalls(string templateExpr) =>
        CountFunctionRegexInstance.Replace(
            input: templateExpr,
            evaluator: static string (m) =>
                m.Groups[groupnum: 1]
                    .Value.Split(separator: ',')
                    .Count(predicate: static bool (s) => !string.IsNullOrWhiteSpace(value: s))
                    .ToString()
        );

    private static void ExtractSubQueries(string templateExpr, List<string> subQueries)
    {
        foreach (Match m in SubQueryPatternRegexInstance.Matches(input: templateExpr))
        {
            if (m.Value.StartsWith(value: '#'))
            {
                if (!subQueries.Contains(item: m.Value))
                    subQueries.Add(item: m.Value);
                continue;
            }

            var startIndex = m.Index;
            var currentIndex = startIndex + m.Length;

            var openParenCount = 0;
            var hasReachedTilde = false;
            var inQuote = false;

            while (currentIndex < templateExpr.Length)
            {
                var (flowControl, value) = HandleCharInFilterExpression(
                    templateExpr: templateExpr,
                    currentIndex: currentIndex,
                    openParenCount: openParenCount,
                    hasReachedTilde: hasReachedTilde,
                    inQuote: inQuote
                );

                if (!flowControl)
                    break;

                openParenCount = value.openParenCount;
                hasReachedTilde = value.hasReachedTilde;
                inQuote = value.inQuote;

                currentIndex++;
            }

            var subQuery = templateExpr[startIndex..currentIndex].Trim();
            if (!subQueries.Contains(item: subQuery))
                subQueries.Add(item: subQuery);
        }

        return;

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
            var regex = new Regex(
                pattern: pattern,
                options: RegexOptions.IgnoreCase,
                matchTimeout: TimeSpan.FromMilliseconds(value: 100)
            );

            while (true)
            {
                var match = regex.Match(input: templateExpr);
                if (!match.Success)
                    break;

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

            LogWriter(text: $"Lookup field '#{key}' not found in processed results.");
            return null;
        }

        var parts = queryStr.Split(separator: '~');
        var pathPart = parts[0];
        var filterPart = parts.Length > 1 ? parts[1] : null;
        var sortPart = parts.Length > 2 ? parts[2] : null;

        var match = ArrayFieldQueryPattern.Match(input: pathPart);
        if (!match.Success)
        {
            LogWriter(text: $"Invalid query format or path not recognized: '{queryStr}'.");
            return null;
        }

        var arrayName = match.Groups[groupnum: 1].Value;
        var fieldName = match.Groups[groupnum: 2].Value;
        var indexCsv = match.Groups[groupnum: 3].Value;

        // 2. Handling array or object
        var token = _sourceData.SelectToken(path: arrayName);

        switch (token)
        {
            case JArray targetArray:
                return Projected(
                    targetArray: targetArray,
                    filterPart: filterPart,
                    sortPart: sortPart,
                    fieldName: fieldName,
                    indexCsv: indexCsv
                );
            case JObject singleObject:
                // Handle single object as array with one element
                return Projected(
                    targetArray: new JArray(content: singleObject),
                    filterPart: filterPart,
                    sortPart: sortPart,
                    fieldName: fieldName,
                    indexCsv: indexCsv
                );
            default:
                LogWriter(
                    text: $"Array or object '{arrayName}' not found in source JSON for query: '{queryStr}'."
                );
                return null;
        }
    }

    private static LightweightTable CreateFlattenedDataTable(JArray array)
    {
        var table = new LightweightTable();
        var columnSet = new HashSet<string>(comparer: StringComparer.OrdinalIgnoreCase);

        // First pass: Collect all column names
        foreach (var item in array.Children<JObject>())
        {
            foreach (var prop in item.Properties())
            {
                if (prop.Value is JObject nestedObj)
                {
                    foreach (var nestedProp in nestedObj.Properties())
                    {
                        var columnName = $"{prop.Name}_{nestedProp.Name}";
                        columnSet.Add(item: columnName);
                    }
                }
                else
                {
                    columnSet.Add(item: prop.Name);
                }
            }
        }

        table.Columns.AddRange(collection: columnSet);

        // Second pass: Populate rows
        foreach (var item in array.Children<JObject>())
        {
            var row = new LightweightRow();
            PopulateRow(row: row, item: item, columns: columnSet);
            table.Rows.Add(item: row);
        }

        return table;
    }

    private static void PopulateRow(LightweightRow row, JObject item, HashSet<string> columns)
    {
        foreach (var prop in item.Properties())
        {
            if (prop.Value is JObject nestedObj)
            {
                foreach (var nestedProp in nestedObj.Properties())
                {
                    var columnName = $"{prop.Name}_{nestedProp.Name}";
                    if (columns.Contains(item: columnName))
                        row[column: columnName] =
                            nestedProp.Value.ToObject<object>() ?? DBNull.Value;
                }
            }
            else
            {
                var columnName = prop.Name;
                if (columns.Contains(item: columnName))
                {
                    row[column: columnName] = prop.Value.ToObject<object>() ?? DBNull.Value;
                }
            }
        }
    }

    private static object? ExtractItemsByIndices(string indexCsv, List<object?> projected)
    {
        if (string.IsNullOrWhiteSpace(value: indexCsv))
            return projected.Count == 1 ? projected[index: 0] : projected;

        var indices = indexCsv
            .Split(separator: ',')
            .Select(selector: s => s.Trim())
            .Where(predicate: s => !string.IsNullOrEmpty(value: s))
            .Select(selector: s => int.TryParse(s: s, result: out var i) ? i : -1)
            .Where(predicate: i => i >= 0 && i < projected.Count)
            .ToList();

        if (indices.Count == 0)
            return null;

        var results = indices.Select(selector: i => projected[index: i]).ToList();

        return results.Count == 1 ? results[index: 0] : results;
    }

    private object? Projected(
        JArray targetArray,
        string? filterPart,
        string? sortPart,
        string fieldName,
        string indexCsv
    )
    {
        try
        {
            using var table = CreateFlattenedDataTable(array: targetArray);

            // Convert fieldName to column name
            var columnName = fieldName.Contains(value: '.')
                ? fieldName.Replace(oldChar: '.', newChar: '_')
                : fieldName;

            // Verify column exists
            if (
                !table.Columns.Contains(
                    value: columnName,
                    comparer: StringComparer.OrdinalIgnoreCase
                )
            )
            {
                LogWriter(
                    text: $"Column '{columnName}' not found. Available columns: {string.Join(separator: ", ", values: table.Columns)}"
                );
                return null;
            }

            // Apply filtering and sorting
            var filteredRows = ApplyFilterAndSort(
                table: table,
                filterPart: filterPart,
                sortPart: sortPart
            );

            // Extract values
            var projected = filteredRows
                .Select(selector: row => row[column: columnName])
                .Where(predicate: value => value != DBNull.Value)
                .ToList();

            // Handle indices
            if (!string.IsNullOrEmpty(value: indexCsv))
            {
                return ExtractItemsByIndices(indexCsv: indexCsv, projected: projected) ?? projected;
            }

            return projected.Count switch
            {
                0 => null,
                1 => projected[index: 0],
                _ => projected,
            };
        }
        catch (Exception ex)
        {
            LogWriter(text: $"Error in Projected: {ex.Message}");
            return null;
        }
    }

    private static List<LightweightRow> ApplyFilterAndSort(
        LightweightTable table,
        string? filterPart,
        string? sortPart
    )
    {
        var rows = table.Rows.AsEnumerable();

        // Apply filter if exists
        if (!string.IsNullOrWhiteSpace(value: filterPart))
        {
            // Implement simple filtering logic
            // Note: You might need to implement a more robust filter parser
            rows = rows.Where(predicate: row =>
                EvaluateFilter(row: row, filter: filterPart, columns: table.Columns)
            );
        }

        // Apply sort if exists
        if (!string.IsNullOrWhiteSpace(value: sortPart))
        {
            // Implement simple sorting logic
            var parts = sortPart.Split(
                separator: ' ',
                options: StringSplitOptions.RemoveEmptyEntries
            );
            if (parts.Length >= 1)
            {
                var sortColumn = parts[0];
                var ascending =
                    parts.Length < 2
                    || !parts[1]
                        .Equals(value: "DESC", comparisonType: StringComparison.OrdinalIgnoreCase);

                rows = ascending
                    ? rows.OrderBy(keySelector: row => row[column: sortColumn])
                    : rows.OrderByDescending(keySelector: row => row[column: sortColumn]);
            }
        }

        return rows.ToList();
    }

    private static bool EvaluateFilter(LightweightRow row, string filter, List<string> columns)
    {
        // Implementasi sederhana filter evaluator
        // Ini bisa diperluas sesuai kebutuhan
        var conditions = filter.Split(
            separator: [" AND ", " OR "],
            options: StringSplitOptions.RemoveEmptyEntries
        );
        return !(
            from condition in conditions
            where condition.Contains(value: '=')
            let parts = condition.Split(separator: '=')
            where parts.Length == 2
            let col = parts[0].Trim()
            let value = parts[1].Trim().Trim(trimChar: '\'')
            where columns.Contains(value: col, comparer: StringComparer.OrdinalIgnoreCase)
            let rowValue = row[column: col]?.ToString()
            where rowValue != value
            select new { }
        ).Any();
    }

    private object? ProcessMathOrQuery(string queryStr)
    {
        // Handle aggregate functions first
        var aggregateMatch = AggregateFunctionPatternInstance.Match(input: queryStr);
        if (aggregateMatch.Success)
        {
            var funcName = aggregateMatch.Groups[groupnum: 1].Value.ToLowerInvariant();
            var innerQuery = aggregateMatch.Groups[groupnum: 2].Value.Trim();

            var result = EvaluateQuery(queryStr: innerQuery);
            if (result is IEnumerable<object> collection)
            {
                if (
                    AggregateFunction.TryApply(
                        functionName: funcName,
                        values: collection,
                        result: out var aggregateResult
                    )
                )
                    return aggregateResult;
            }
            else if (
                result != null
                && AggregateFunction.TryApply(
                    functionName: funcName,
                    values: [result],
                    result: out var aggregateResult
                )
            )
                return aggregateResult;

            return 0;
        }

        // Normalisasi nama fungsi karna mathEval tidak memiliki fungsi
        // dengan nama AVG, yang ada adalah AVERAGE
        var templateExpr = AvgRegexInstance.Replace(input: queryStr, replacement: "AVERAGE(");

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
        var countMatch = FullCountExpressionRegexInstance.Match(input: templateExpr);
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

    private void LogWriter(ReadOnlySpan<char> text)
    {
        // 1. Log ke console (jika diperlukan)
        Console.WriteLine(value: $"[JsonQueryEngine] {text}");

        // 2. Simpan ke buffer untuk performa tinggi
        var logEntry = $"{text}{Environment.NewLine}";

        // Cek apakah cukup ruang di buffer
        if (_logPosition + logEntry.Length > _logBuffer.Length)
        {
            // Buffer penuh, flush dulu
            FlushLogBuffer();
        }

        // Jika setelah flush masih tidak cukup, gunakan StringBuilder
        if (_logPosition + logEntry.Length > _logBuffer.Length)
        {
            _sbLog.Append(value: logEntry);
            return;
        }

        // Copy ke buffer
        logEntry.AsSpan().CopyTo(destination: _logBuffer.AsSpan(start: _logPosition));
        _logPosition += logEntry.Length;
    }

    private void FlushLogBuffer()
    {
        if (_logPosition > 0)
        {
            _sbLog.Append(value: _logBuffer, startIndex: 0, charCount: _logPosition);
            _logPosition = 0;
        }
    }

    internal async Task SaveLog(
        LogDataModel logData,
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            FlushLogBuffer();

            if (string.IsNullOrEmpty(value: logData.Log))
            {
                logData.Log =
                    _sbLog.Length > 0 ? _sbLog.ToString() : "Process completed successfully.";
            }

            await logData.Save(pgHelper: pgHelper, cancellationToken: cancellationToken);
        }
        finally
        {
            // Reset buffer setelah disimpan
            _sbLog.Clear();
            _logPosition = 0;
        }
    }
}

internal static class AggregateFunction
{
    private static readonly Dictionary<string, Func<IEnumerable<object?>, double>> AggregateFuncs =
        new(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {
                "sum",
                static values =>
                    values
                        .Where(predicate: static v => v != null)
                        .Sum(selector: static v => Convert.ToDouble(value: v))
            },
            {
                "avg",
                static values =>
                    values
                        .Where(predicate: static v => v != null)
                        .Average(selector: static v => Convert.ToDouble(value: v))
            },
            {
                "min",
                static values =>
                    values
                        .Where(predicate: static v => v != null)
                        .Min(selector: static v => Convert.ToDouble(value: v))
            },
            {
                "max",
                static values =>
                    values
                        .Where(predicate: static v => v != null)
                        .Max(selector: static v => Convert.ToDouble(value: v))
            },
            { "count", static values => values.Count(predicate: static v => v != null) },
        };

    public static bool TryApply(string functionName, IEnumerable<object?> values, out double result)
    {
        if (AggregateFuncs.TryGetValue(key: functionName, value: out var func))
        {
            try
            {
                result = func(arg: values);
                return true;
            }
            catch
            {
                // Handle empty collection cases
                result = 0;
                return false;
            }
        }
        result = 0;
        return false;
    }
}
