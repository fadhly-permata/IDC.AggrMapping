using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using System.Text.Json.Serialization;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Models;

internal class DestinationConfig
{
    [JsonProperty(propertyName: "db_type"), JsonPropertyName(name: "db_type"), Required]
    internal string DbType { get; set; } = string.Empty;

    [JsonProperty(propertyName: "db_name"), JsonPropertyName(name: "db_name"), Required]
    internal string DbName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database schema.
    /// </summary>
    [JsonProperty(propertyName: "schema")]
    [Required]
    internal string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    [JsonProperty(propertyName: "table")]
    [Required]
    internal string Table { get; set; } = string.Empty;
}

internal class FieldMapping
{
    [
        JsonProperty(propertyName: "is_primary_key"),
        JsonPropertyName(name: "is_primary_key"),
        Required
    ]
    internal bool IsPrimaryKey { get; set; }

    [JsonProperty(propertyName: "data_type"), JsonPropertyName(name: "data_type"), Required]
    internal string DataType { get; set; } = "unknown";

    [JsonProperty(propertyName: "source_field"), JsonPropertyName(name: "source_field"), Required]
    internal string SourceField { get; set; } = string.Empty;

    [
        JsonProperty(propertyName: "destination_column"),
        JsonPropertyName(name: "destination_column"),
        Required
    ]
    internal string DestinationColumn { get; set; } = string.Empty;

    internal bool RequiresQuotation()
    {
        return DataType.ToLower() switch
        {
            "varchar" or "text" or "character varying" => true,
            "char" or "character" or "bpchar" => true,
            "date" or "timestamp" or "timestamp with time zone" or "timestamp without time zone" =>
                true,
            "time" or "time with time zone" or "time without time zone" => true,
            "interval" => true,
            "json" or "jsonb" => true,
            "uuid" => true,
            "inet" or "cidr" or "macaddr" => true,
            "money" => true,
            "bytea" => true,
            _ => false,
        };
    }
}

internal class MappingGroup
{
    [JsonProperty(propertyName: "destination")]
    [Required]
    internal DestinationConfig Destination { get; set; } = new();

    [JsonProperty(propertyName: "operation")]
    [Required]
    internal string Operation { get; set; } = string.Empty;

    [
        JsonProperty(propertyName: "field_mappings"),
        JsonPropertyName(name: "field_mappings"),
        Required,
        MinLength(length: 1)
    ]
    internal List<FieldMapping> FieldMappings { get; set; } = [];
}

internal class GroupedMappingModel : BaseModel<GroupedMappingModel>
{
    [
        JsonProperty(propertyName: "map_code"),
        JsonPropertyName(name: "map_code"),
        Required(AllowEmptyStrings = false)
    ]
    internal string MapCode { get; set; } = string.Empty;

    [
        JsonProperty(propertyName: "grouped_mappings"),
        JsonPropertyName(name: "grouped_mappings"),
        Required,
        MinLength(length: 1)
    ]
    internal List<MappingGroup> GroupedMappings { get; set; } = [];

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
    internal async Task<GroupedMappingModel?> Load(
        SystemLogging systemLogging,
        PostgreHelper pgHelper,
        Caching caching,
        string mapCode,
        CancellationToken cancellationToken = default
    )
    {
        SetLogger(logger: systemLogging);

        var result =
            await caching.GetOrSetAsync(
                key: $"GroupedMappingModel-{mapCode}",
                valueFactory: async () =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
                    (_, var result) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "get_map_configs",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_map_code",
                                    Value = mapCode,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: cancellationToken
                    );

                    return result != null ? JObject.Parse(json: result.ToString() ?? "{}") : null;
                },
                expirationRenewal: true
            ) ?? throw new DataException(s: $"Map configuration with code {mapCode} not found.");

        MapProperties(jObject: result);
        await Validate(caching: caching, pgHelper: pgHelper, cancellationToken: cancellationToken);

        return this;
    }

    internal async Task Validate(
        Caching caching,
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        MapCode.ThrowIfNullOrWhitespace(paramName: nameof(MapCode));

        var gcm = await new GlobalConfigurationModel().GetGlobalConfigurationsAsync(
            pgHelper: pgHelper,
            caching: caching,
            cancellationToken: cancellationToken
        );

        if (GroupedMappings.Count > gcm.MaxGroupMapCount)
            throw new DataException(
                s: $"Can not have more than {gcm.MaxGroupMapCount} groups on map configuration."
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

            var hasPrimaryKey = false;

            foreach (var field in group.FieldMappings)
            {
                field.SourceField.ThrowIfNullOrWhitespace(paramName: nameof(field.SourceField));
                field.DestinationColumn.ThrowIfNullOrWhitespace(
                    paramName: nameof(field.DestinationColumn)
                );
                field.DataType.ThrowIfNullOrWhitespace(paramName: nameof(field.DataType));
                if (field.DataType == "unknown")
                    throw new DataException(s: "Can not have 'unknown' data type.");

                hasPrimaryKey |= field.IsPrimaryKey;
            }

            if (!hasPrimaryKey)
                throw new DataException(s: "Can not find primary key field.");
        }
    }

    internal async Task<StringBuilder> DoUpsert(
        UpsertAndAggregatePayloadModel payload,
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        var log = new StringBuilder();
        var result = new List<string>();

        try
        {
            var payloadData = payload.Data is IEnumerable<object> data
                ? data.ToList()
                : throw new DataException("Payload data is not an enumerable collection.");

            foreach (var group in GroupedMappings)
            {
                var groupIdentifier = $"{group.Destination.Schema}.{group.Destination.Table}";
                var itemIndex = 0;

                try
                {
                    foreach (var dataItem in payloadData)
                    {
                        itemIndex++;
                        try
                        {
                            var jObject = JObject.FromObject(dataItem);
                            var columns = new List<string>();
                            var values = new List<string>();
                            var updateSet = new List<string>();
                            var hasValidData = false;

                            // Process each field mapping
                            foreach (var field in group.FieldMappings)
                            {
                                var token = jObject[field.SourceField];
                                if (token == null || token.Type == JTokenType.Null)
                                    continue;

                                var value = token.ToString();
                                if (string.IsNullOrWhiteSpace(value))
                                    continue;

                                columns.Add(field.DestinationColumn);
                                values.Add(
                                    field.RequiresQuotation()
                                        ? $"'{value.Replace("'", "''")}'"
                                        : value
                                );

                                // Add to update set for non-primary key fields
                                if (!field.IsPrimaryKey)
                                {
                                    updateSet.Add(
                                        field.RequiresQuotation()
                                            ? $"{field.DestinationColumn} = '{value.Replace("'", "''")}'"
                                            : $"{field.DestinationColumn} = {value}"
                                    );
                                }

                                hasValidData = true;
                            }

                            if (!hasValidData)
                            {
                                result.Add($"{itemIndex}: No valid columns found");
                                continue;
                            }

                            var primaryKeyColumn =
                                group
                                    .FieldMappings.FirstOrDefault(x => x.IsPrimaryKey)
                                    ?.DestinationColumn
                                ?? throw new InvalidOperationException(
                                    "Primary key column not found"
                                );

                            var onConflictAction =
                                updateSet.Count > 0
                                    ? $"DO UPDATE SET {string.Join(", ", updateSet)}"
                                    : "DO NOTHING";

                            var strQuery = $"""
                                INSERT INTO {group.Destination.Schema}.{group.Destination.Table}
                                ({string.Join(", ", columns)})
                                VALUES
                                ({string.Join(", ", values)})
                                ON CONFLICT ({primaryKeyColumn})
                                {onConflictAction}
                                RETURNING 1;
                                """;

                            // Always use dblink for all database connections
                            var dbName = group.Destination.DbName;
                            if (dbName.StartsWith("idc."))
                            {
                                dbName = dbName.Substring(4);
                            }

                            var finalQuery = $"""
                                SELECT t.result
                                FROM public.get_dblink_constring('{dbName}') AS conn_str
                                CROSS JOIN LATERAL
                                    dblink(
                                        conn_str, 
                                        '{strQuery.Replace("'", "''")}'
                                    ) as t(result int);
                                """;

                            await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
                            await pgHelper.ExecuteScalarAsync(
                                finalQuery,
                                _ => { },
                                cancellationToken
                            );

                            result.Add($"{itemIndex}: Process completed successfully");
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            result.Add($"{itemIndex}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result.Add($"{itemIndex}: {groupIdentifier} - {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.Add($"FATAL_ERROR: {ex.Message}");
        }

        // Format output sebagai array JSON
        log.AppendLine("[");
        if (result.Count > 0)
        {
            log.AppendLine(
                "  \""
                    + string.Join("\",\n  \"", result.Select(r => r.Replace("\"", "\\\"")))
                    + "\""
            );
        }
        log.AppendLine("]");

        return log;
    }
}
