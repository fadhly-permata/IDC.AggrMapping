using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;
using IDC.AggrMapping.Utilities.Data;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;

namespace IDC.AggrMapping.Utilities.Models.Postgre;

/// <summary>
/// Represents a grouped mapping configuration for data transformation operations.
/// </summary>
public class GroupedMappingModel : BaseModel<GroupedMappingModel>
{
    /// <summary>
    ///     Gets or sets the map code.
    /// </summary>
    [
        JsonProperty(propertyName: "map_code"),
        JsonPropertyName(name: "map_code"),
        Required(AllowEmptyStrings = false)
    ]
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the collection of grouped mappings.
    /// </summary>
    [
        JsonProperty(propertyName: "grouped_mappings"),
        JsonPropertyName(name: "grouped_mappings"),
        Required,
        MinLength(length: 1)
    ]
    public List<MappingGroup> GroupedMappings { get; set; } = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="GroupedMappingModel"/> class.
    /// </summary>
    /// <param name="systemLogging">
    ///     The system logging instance.
    /// </param>
    /// <param name="pgHelper">
    ///     The PostgreHelper instance.
    /// </param>
    /// <param name="caching">
    ///     The caching instance.
    /// </param>
    /// <param name="mapCode">
    ///     The key.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token.
    /// </param>
    /// <returns>
    ///     A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task<GroupedMappingModel?> Load(
        SystemLogging systemLogging,
        PostgreHelper pgHelper,
        Caching caching,
        string mapCode,
        CancellationToken cancellationToken = default
    )
    {
        SetLogger(logger: systemLogging);

        var result = await caching.GetOrSetAsync(
            key: $"GroupedMappingModel-{mapCode}",
            valueFactory: async () =>
                await pgHelper.GetGroupedMappingAsync(
                    mapCode: mapCode,
                    cancellationToken: cancellationToken
                ),
            expirationRenewal: true
        );

        if (result is not null)
        {
            MapProperties(jObject: result);
            await this.WriteSuccessLog(
                mapCode: mapCode,
                systemLogging: systemLogging,
                cancellationToken: cancellationToken
            );
            await Validate(
                caching: caching,
                pgHelper: pgHelper,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await this.WriteFailLog(
                mapCode: mapCode,
                systemLogging: systemLogging,
                cancellationToken: cancellationToken
            );
        }

        return this;
    }

    private async Task Validate(
        Caching caching,
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        MapCode.ThrowIfNullOrWhitespace(nameof(MapCode));

        var gcm = await new GlobalConfigurationModel().InitFromDatabase(
            pgHelper: pgHelper,
            caching: caching,
            cancellationToken: cancellationToken
        );

        if (GroupedMappings.Count > gcm.MaxGroupMapCount)
            throw new DataException(
                $"Can not have more than {gcm.MaxGroupMapCount} groups on map configuration."
            );

        foreach (var group in GroupedMappings)
        {
            group.Destination.DbName.ThrowIfNullOrWhitespace(
                paramName: nameof(group.Destination.DbName)
            );
            group.Destination.Schema.ThrowIfNullOrWhitespace(
                paramName: nameof(group.Destination.Schema)
            );
            group.Destination.Table.ThrowIfNullOrWhitespace(
                paramName: nameof(group.Destination.Table)
            );

            var hasPK = false;

            foreach (var field in group.FieldMappings)
            {
                field.SourceField.ThrowIfNullOrWhitespace(paramName: nameof(field.SourceField));
                field.DestinationColumn.ThrowIfNullOrWhitespace(
                    paramName: nameof(field.DestinationColumn)
                );
                field.DataType.ThrowIfNullOrWhitespace(paramName: nameof(field.DataType));
                if (field.DataType == "unknown")
                    throw new DataException("Can not have 'unknown' data type.");

                hasPK |= field.IsPrimaryKey;
            }

            if (!hasPK)
                throw new DataException("Can not find primary key field.");
        }
    }

    /// <summary>
    ///     Does the upsert.
    /// </summary>
    /// <param name="payload">
    ///     The payload.
    /// </param>
    /// <param name="pgHelper">
    /// </param>
    /// <param name="caching"></param>
    /// <param name="systemLogging"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DoUpsert(
        MlaPayloadModel payload,
        PostgreHelper pgHelper,
        Caching caching,
        SystemLogging systemLogging,
        CancellationToken cancellationToken = default
    )
    {
        if (payload?.Data == null)
            return;

        foreach (var group in GroupedMappings)
        {
            // Membuat parameter untuk setiap field mapping
            var columns = new List<string>();
            var valuesList = new List<string>();

            foreach (var dataItem in payload.Data)
            {
                if (dataItem == null)
                    continue;

                var values = new List<string>();
                var rowColumns = new List<string>();

                foreach (var field in group.FieldMappings)
                {
                    // Dapatkan nilai dari nested property
                    var value = GetNestedPropertyValue(dataItem, field.SourceField);

                    // Skip jika nilai null atau string kosong
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                        continue;

                    rowColumns.Add(field.DestinationColumn);

                    if (field.RequiresQuotation())
                    {
                        values.Add(
                            $"'{value.ToString()?.Replace("'", "''", StringComparison.Ordinal)}'"
                        );
                    }
                    else
                    {
                        values.Add(value.ToString() ?? string.Empty);
                    }
                }

                // Pastikan ada kolom yang akan diinsert
                if (rowColumns.Count > 0)
                {
                    // Untuk baris pertama, set columns
                    if (columns.Count == 0)
                        columns = rowColumns;

                    valuesList.Add($"({string.Join(", ", values)})");
                }
            }

            // Jika tidak ada data yang valid, skip
            if (columns.Count == 0 || valuesList.Count == 0)
                continue;

            // Buat bagian UPDATE SET hanya untuk field yang ada nilainya
            var updateSet = new List<string>();
            var firstData = payload.Data.FirstOrDefault();
            if (firstData != null)
            {
                foreach (var field in group.FieldMappings)
                {
                    // Primary key tidak diupdate
                    if (field.IsPrimaryKey)
                        continue;

                    var value = GetNestedPropertyValue(firstData, field.SourceField);

                    // Skip jika nilai null
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                        continue;

                    var valueStr = value.ToString();
                    if (string.IsNullOrEmpty(valueStr))
                        continue;

                    if (field.RequiresQuotation())
                    {
                        updateSet.Add(
                            $"{field.DestinationColumn} = '{valueStr.Replace("'", "''", StringComparison.Ordinal)}'"
                        );
                    }
                    else
                    {
                        updateSet.Add($"{field.DestinationColumn} = {valueStr}");
                    }
                }
            }

            // Jika tidak ada field yang akan diupdate, gunakan DO NOTHING
            var onConflictAction =
                updateSet.Count > 0
                    ? $"DO UPDATE SET {string.Join(", ", updateSet)}"
                    : "DO NOTHING";

            var primaryKeyColumn = group
                .FieldMappings.FirstOrDefault(x => x.IsPrimaryKey)
                ?.DestinationColumn;

            if (string.IsNullOrEmpty(primaryKeyColumn))
            {
                systemLogging.LogWarning(
                    $"Primary key not found for group: {group.Destination.Schema}.{group.Destination.Table}"
                );
                continue;
            }

            var strQuery =
                $@"
                    INSERT INTO
                        {group.Destination.Schema}.{group.Destination.Table}
                        ({string.Join(", ", columns)})
                    VALUES
                        {string.Join(",\n", valuesList)}
                    ON CONFLICT 
                        ({primaryKeyColumn})
                    {onConflictAction}
                    RETURNING 
                        1;
                ";

            // kalo destination.dbname bukan idc.kaml, wrap strQuery dengan dblink
            if (
                !string.Equals(
                    group.Destination.DbName,
                    "idc.kaml",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var dbName = group.Destination.DbName.Replace(
                    "idc.",
                    "",
                    StringComparison.OrdinalIgnoreCase
                );
                strQuery =
                    $@"
                SELECT 
                    dblink(
                        public.get_dblink_constring('{dbName}'), 
                        '{strQuery?.Replace("'", "''", StringComparison.Ordinal)}'
                    )
            ";
            }

            systemLogging.LogInformation($"[ Generated Query ]\n{strQuery}");

            try
            {
                await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
                (_, var result) = await pgHelper.ExecuteScalarAsync(
                    query: strQuery ?? string.Empty,
                    callback: data => { },
                    cancellationToken: cancellationToken
                );

                if (result?.ToString() == "1")
                {
                    await this.WriteSuccessLog(
                        mapCode: MapCode,
                        systemLogging: systemLogging,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await this.WriteFailLog(
                        mapCode: MapCode,
                        systemLogging: systemLogging,
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                systemLogging.LogError($"Error executing query: {ex.Message}");
                await this.WriteFailLog(
                    mapCode: MapCode,
                    systemLogging: systemLogging,
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    private static object? GetNestedPropertyValue(object? obj, string? propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return null;

        object? current = obj;
        var properties = propertyPath.Split('.');

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            if (current == null)
                return null;

            var type = current.GetType();
            var propInfo = type.GetProperty(prop);

            if (propInfo == null)
                return null;

            current = propInfo.GetValue(current);

            if (
                current is System.Collections.IEnumerable enumerable
                && current is not string
                && current.GetType() != typeof(byte[])
                && i < properties.Length - 1
            )
            {
                current = enumerable.Cast<object?>().FirstOrDefault();
            }
        }

        return current;
    }
}
