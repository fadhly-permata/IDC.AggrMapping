using System.Data;
using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Controller Data Aggregation for Frontend
/// </summary>
[Route(template: "AggrMapping/AggregateConfig")]
[ApiController]
public class AggregateConfigForFe(SystemLogging systemLogging, PostgreHelper pgHelper)
    : ControllerBase
{
    /// <summary>
    ///     Retrieves a paginated list of aggregation configurations based on the specified filter type
    /// </summary>
    /// <remarks>
    ///     This endpoint returns a collection of aggregation configurations filtered by the specified grid type.
    ///     The response is formatted as a JSON array containing configuration objects.
    ///
    ///     Sample request:
    ///     ```
    ///     GET /AggrMapping/AggregateConfig/DataGrid/all
    ///     GET /AggrMapping/AggregateConfig/DataGrid/last_active
    ///     GET /AggrMapping/AggregateConfig/DataGrid/last_version
    ///     ```
    /// </remarks>
    /// <param name="gridType">
    ///     The type of filter to apply when retrieving configurations.
    ///
    ///     Available values:
    ///     - all: Returns all available configurations (default)
    ///     - last_active: Returns only configurations that are currently active
    ///     - last_version: Returns only the most recent version of each configuration
    ///
    ///     Example: LAST_ACTIVE
    /// </param>
    /// <param name="cancellationToken">
    ///     A token that can be used to cancel the operation
    /// </param>
    /// <returns>
    ///     An <see cref="APIResponseData{T}"/> containing:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Error message if status is "Failed"
    ///     - Data: <see cref="JArray"/> of aggregation configurations
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpGet(template: "DataGrid/{gridType}")]
    public async Task<APIResponseData<JArray?>> DataGrid(
        [FromRoute] string? gridType = "all",
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            gridType.ThrowIfNullOrWhitespace(
                paramName: nameof(gridType),
                message: "Grid type cannot be null, empty or whitespace."
            );

            var allowedGridTypes = new[] { "all", "last_active", "last_version" };
            if (
                !allowedGridTypes.Contains(
                    value: gridType,
                    comparer: StringComparer.OrdinalIgnoreCase
                )
            )
                throw new ArgumentException(
                    paramName: nameof(gridType),
                    message: $"Grid type must be one of: {string.Join(separator: ", ", value: allowedGridTypes)}."
                );

            return await new APIResponseData<JArray?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await Task.CompletedTask;

                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_get_aggregation_grid",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter { Name = "p_mode", Value = gridType },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JArray.Parse(json: data as string ?? "[]");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<JArray?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Retrieves detailed information for a specific aggregation configuration
    /// </summary>
    /// <remarks>
    ///     This endpoint returns the complete details of an aggregation configuration identified by its unique ID.
    ///     The response includes all configuration properties and settings.
    ///
    ///     Sample request:
    ///     ```
    ///     POST /AggrMapping/AggregateConfig/Detail
    ///     {
    ///         "Id": 12345
    ///     }
    ///     ```
    /// </remarks>
    /// <param name="payload">
    ///     The request payload containing the ID of the aggregation configuration to retrieve.
    ///
    ///     Properties:
    ///     - Id (int): The unique identifier of the aggregation configuration. Must be greater than 0.
    /// </param>
    /// <param name="cancellationToken">
    ///     A token that can be used to cancel the operation
    /// </param>
    /// <returns>
    ///     An <see cref="APIResponseData{T}"/> containing:
    ///     - Status: "Success" or "Failed"
    ///     - Data: <see cref="JObject"/> with the aggregation configuration details
    ///     - Message: Error message if status is "Failed"
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when the provided ID is less than 1
    /// </exception>
    /// <exception cref="DataException">
    ///     Thrown when no configuration is found with the specified ID
    /// </exception>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "Detail")]
    public async Task<APIResponseData<JObject?>> Detail(
        AggConfigFeModel.IdOnly payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_get_aggregation_detail",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = payload.Id,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(
                        json: data as string
                            ?? throw new DataException(
                                s: $"Aggregate configuration with id '{payload.Id}' not found."
                            )
                    );
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Marks an aggregation configuration as deleted in the system
    /// </summary>
    /// <remarks>
    ///     This endpoint performs a soft delete of the specified aggregation configuration.
    ///     The operation records the user who performed the deletion and the timestamp.
    ///
    ///     Sample request:
    ///     ```
    ///     POST /AggrMapping/AggregateConfig/AggrConfigIdAndUser/Delete
    ///     {
    ///         "Id": 12345,
    ///         "User": "username@example.com"
    ///     }
    ///     ```
    /// </remarks>
    /// <param name="payload">
    ///     The request payload containing:
    ///     - Id (int): The unique identifier of the aggregation configuration to delete
    ///     - User (string): The username or email of the user performing the deletion
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Operation result message
    ///     - Data: JObject containing deletion confirmation (usually null on success)
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Delete")]
    public async Task<APIResponseData<JObject?>> Delete(
        AggConfigFeModel.IdAndUser payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_remove_aggregation_data",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = payload.Id,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_username",
                                    Value = payload.User,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Rolls back an aggregation configuration to its previous version
    /// </summary>
    /// <remarks>
    ///     This endpoint restores the previous version of the specified aggregation configuration.
    ///     The operation is recorded with the user who performed the rollback.
    ///
    ///     Sample request:
    ///     ```
    ///     POST /AggrMapping/AggregateConfig/AggrConfigIdAndUser/Rollback
    ///     {
    ///         "Id": 12345,
    ///         "User": "username@example.com"
    ///     }
    ///     ```
    /// </remarks>
    /// <param name="payload">
    ///     The request payload containing:
    ///     - Id (int): The unique identifier of the aggregation configuration to rollback
    ///     - User (string): The username or email of the user performing the rollback
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Operation result message
    ///     - Data: JObject containing the rolled back configuration status
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Rollback")]
    public async Task<APIResponseData<JObject?>> Rollback(
        AggConfigFeModel.IdAndUser payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_rollback_aggregation",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = payload.Id,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_user",
                                    Value = payload.User,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Retrieves audit log entries for a specific aggregation configuration
    /// </summary>
    /// <remarks>
    ///     This endpoint returns the complete audit trail for the specified aggregation configuration,
    ///     including all changes, deletions, and other relevant activities.
    ///
    ///     Sample request:
    ///     ```
    ///     POST /AggrMapping/AggregateConfig/AggrConfigIdAndUser/Log
    ///     {
    ///         "Code": "CONFIG_001"
    ///     }
    ///     ```
    /// </remarks>
    /// <param name="payload">
    ///     The request payload containing:
    ///     - Code (string): The unique code of the aggregation configuration
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Error message if status is "Failed"
    ///     - Data: JObject containing the log entries for the configuration
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Log")]
    public async Task<APIResponseData<JObject?>> Log(
        AggConfigFeModel.CodeOnly payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_log_aggregation",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_code",
                                    Value = payload.Code,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: e,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Creates a duplicate of an existing aggregation configuration
    /// </summary>
    /// <remarks>
    ///     This endpoint creates a new configuration by copying an existing one,
    ///     with the option to specify a new name for the copy.
    ///
    ///     Sample request:
    ///     ```
    ///     POST /AggrMapping/AggregateConfig/AggrConfigIdAndUser/Copy
    ///     {
    ///         "Id": 12345,
    ///         "Name": "New Configuration Copy",
    ///         "User": "username@example.com"
    ///     }
    ///     ```
    /// </remarks>
    /// <param name="payload">
    ///     The request payload containing:
    ///     - Id (int): The ID of the configuration to copy
    ///     - Name (string): The new name for the copied configuration
    ///     - User (string): The username performing the copy operation
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Operation result message
    ///     - Data: JObject containing the new configuration details
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Copy")]
    public async Task<APIResponseData<JObject?>> Copy(
        AggConfigFeModel.CopyData payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_copy_aggregation",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = payload.Id,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_name",
                                    Value = payload.Name,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_user",
                                    Value = payload.User,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: e,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Processes an approval or rejection for an aggregation configuration
    /// </summary>
    /// <remarks>
    ///     This endpoint handles the approval workflow for configuration changes,
    ///     allowing authorized users to approve, reject, or request changes.
    ///
    ///     Sample request:
    ///     ```
    ///     POST /AggrMapping/AggregateConfig/AggrConfigIdAndUser/Approval
    ///     {
    ///         "Id": 12345,
    ///         "Username": "approver@example.com",
    ///         "Role": "Senior Reviewer",
    ///         "Action": "Approve",
    ///         "Note": "Configuration meets all requirements"
    ///     }
    ///     ```
    /// </remarks>
    /// <param name="payload">
    ///     The approval details containing:
    ///     - Id (int): The configuration ID being processed
    ///     - Username (string): The approver's username
    ///     - Role (string): The approver's role in the process
    ///     - Action (string): The approval action (e.g., "Approve", "Reject")
    ///     - Note (string): Optional comments about the decision
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for the operation
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Operation result message
    ///     - Data: JObject containing approval result and updated configuration status
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Approval")]
    public async Task<APIResponseData<JObject?>> Approval(
        AggConfigFeModel.AggrConfigApproval payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_approval_proc_aggregation",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = payload.Id,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_user",
                                    Value = payload.User,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_role",
                                    Value = payload.Role,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_action",
                                    Value = payload.Action,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_note",
                                    Value = payload.Note,
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: e,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Creates a new aggregation configuration in the system
    /// </summary>
    /// <remarks>
    ///     This endpoint allows creating a new aggregation configuration with the specified parameters.
    ///     The configuration will be validated against system requirements before being persisted.
    ///
    ///     Sample request:
    ///     ```json
    ///     POST /api/aggregate-config
    ///     Content-Type: application/json
    ///
    ///     {
    ///         "user": "admin@example.com",
    ///         "name": "Sample Configuration",
    ///         "desc": "Sample description",
    ///         "type": "response",
    ///         "data_applied": "{\"key\":\"value\"}",
    ///         "json_list": [
    ///             { "field": "value1" },
    ///             { "field": "value2" }
    ///         ],
    ///         "json_condition": [
    ///             { "condition": "field = value" }
    ///         ],
    ///         "final_config": [
    ///             { "config": "final_value" }
    ///         ]
    ///     }
    ///     ```
    ///
    ///     **Notes:**
    ///     - The user field is required and must be a non-empty string
    ///     - The name field is required and must contain only alphanumeric characters, spaces, and underscores
    ///     - data_applied must be a valid JSON string
    ///     - json_list, json_condition, and final_config must be valid JSON arrays
    /// </remarks>
    /// <param name="payload">
    ///     The aggregation configuration data to be created.
    ///     Must satisfy all validation requirements.
    ///
    ///     Required properties:
    ///     - user (string): The username or identifier of the user creating the configuration
    ///     - name (string): The display name of the configuration
    ///     - data_applied (string): JSON string representing the applied data
    ///
    ///     Optional properties:
    ///     - desc (string): Description of the configuration
    ///     - type (string): Type of configuration (e.g., "response")
    ///     - json_list (JArray): JSON array of list items
    ///     - json_condition (JArray): JSON array of conditions
    ///     - final_config (JArray): JSON array of final configuration
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token that can be used to cancel the asynchronous operation.
    ///     Used to terminate ongoing requests if needed.
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Operation result message
    ///     - Data: JObject containing approval result and updated configuration status
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Insert")]
    public async Task<APIResponseData<JObject?>> Insert(
        AggConfigFeModel.Insert payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_upsert_aggregation",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = null,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_user",
                                    Value = payload.User,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_name",
                                    Value = payload.Name,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_desc",
                                    Value = payload.Desc,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_data_applied",
                                    Value = payload.DataApplied,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_type",
                                    Value = payload.Type,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_json_list",
                                    Value = payload.JsonList?.ToString(),
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_json_condition",
                                    Value = payload.JsonCondition?.ToString(),
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_config_final",
                                    Value = payload.ConfigFinal?.ToString(),
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: e,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }

    /// <summary>
    ///     Updates an existing aggregation configuration in the system
    /// </summary>
    /// <remarks>
    ///     This endpoint allows updating an existing aggregation configuration with new values.
    ///     The configuration will be validated against system requirements before being updated.
    ///
    ///     Sample request:
    ///     ```json
    ///     POST /api/aggregate-config/AggrConfigIdAndUser/Update
    ///     Content-Type: application/json
    ///
    ///     {
    ///         "id": 12345,
    ///         "user": "admin@example.com",
    ///         "name": "Updated Configuration",
    ///         "desc": "Updated description",
    ///         "type": "response",
    ///         "data_applied": "{\"key\":\"updated_value\"}",
    ///         "json_list": [
    ///             { "field": "updated_value1" },
    ///             { "field": "updated_value2" }
    ///         ],
    ///         "json_condition": [
    ///             { "condition": "field = updated_value" }
    ///         ],
    ///         "final_config": [
    ///             { "config": "updated_final_value" }
    ///         ]
    ///     }
    ///     ```
    ///
    ///     **Notes:**
    ///     - The id field is required and must be a positive integer
    ///     - The user field is required and must be a non-empty string
    ///     - The name field is required and must contain only alphanumeric characters, spaces, and underscores
    ///     - data_applied must be a valid JSON string
    ///     - json_list, json_condition, and final_config must be valid JSON arrays
    /// </remarks>
    /// <param name="payload">
    ///     The aggregation configuration data to be updated.
    ///     Must satisfy all validation requirements.
    ///
    ///     Required properties:
    ///     - id (int): The unique identifier of the configuration to update
    ///     - user (string): The username or identifier of the user updating the configuration
    ///     - name (string): The updated display name of the configuration
    ///     - data_applied (string): Updated JSON string representing the applied data
    ///
    ///     Optional properties:
    ///     - desc (string): Updated description of the configuration
    ///     - type (string): Updated type of configuration (e.g., "response")
    ///     - json_list (JArray): Updated JSON array of list items
    ///     - json_condition (JArray): Updated JSON array of conditions
    ///     - final_config (JArray): Updated JSON array of final configuration
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token that can be used to cancel the asynchronous operation.
    ///     Used to terminate ongoing requests if needed.
    /// </param>
    /// <returns>
    ///     APIResponseData with:
    ///     - Status: "Success" or "Failed"
    ///     - Message: Operation result message
    ///     - Data: JObject containing approval result and updated configuration status
    /// </returns>
    [Tags(tags: "Aggregation Config For FE"), HttpPost(template: "AggrConfigIdAndUser/Update")]
    public async Task<APIResponseData<JObject?>> Update(
        AggConfigFeModel.Update payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await new APIResponseData<JObject?>().ChangeData(
                valueFactoryAsync: async (ct) =>
                {
                    await pgHelper.ConnectAsync(cancellationToken: ct);
                    var (_, data) = await pgHelper.ExecuteScalarAsync(
                        spCallInfo: new PostgreHelper.SPCallInfo
                        {
                            Schema = "aggregation",
                            SPName = "acv_upsert_aggregation",
                            Parameters =
                            [
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_id",
                                    Value = payload.Id,
                                    DataType = NpgsqlTypes.NpgsqlDbType.Bigint,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_user",
                                    Value = payload.User,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_name",
                                    Value = payload.Name,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_desc",
                                    Value = payload.Desc,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_data_applied",
                                    Value = payload.DataApplied,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_type",
                                    Value = payload.Type,
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_json_list",
                                    Value = payload.JsonList?.ToString(),
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_json_condition",
                                    Value = payload.JsonCondition?.ToString(),
                                },
                                new PostgreHelper.SPParameter
                                {
                                    Name = "p_config_final",
                                    Value = payload.ConfigFinal?.ToString(),
                                },
                            ],
                        },
                        callback: _ => { },
                        cancellationToken: ct
                    );

                    return JObject.Parse(json: data as string ?? "{}");
                },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            return new APIResponseData<JObject?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: e,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
