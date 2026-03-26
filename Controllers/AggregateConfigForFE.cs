using System.Data;
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
[Route("AggrMapping/AggregateConfig")]
[ApiController]
public class AggregateConfigForFe(SystemLogging systemLogging, PostgreHelper pgHelper)
    : ControllerBase
{
    /// <summary>
    ///     Represents the unique identifier for an aggregation configuration
    /// </summary>
    /// <param name="Id">
    ///     The unique numeric identifier of the aggregation configuration.
    ///     Must be a positive integer greater than 0.
    /// </param>
    public record AggConfigId(int Id);

    /// <summary>
    ///     Represents the unique identifier for an aggregation configuration
    /// </summary>
    /// <param name="Id">
    ///     The unique numeric identifier of the aggregation configuration.
    ///     Must be a positive integer greater than 0.
    /// </param>
    /// <param name="User">
    ///     The user associated with the aggregation configuration.
    ///     Must be a non-empty string.
    /// </param>
    public record AggrConfigIdAndUser(int Id, string User);

    /// <summary>
    ///     Represents an aggregation configuration identifier with additional metadata
    /// </summary>
    /// <param name="Id">
    ///     The unique numeric identifier of the aggregation configuration
    /// </param>
    /// <param name="Name">
    ///     The display name or title of the aggregation configuration
    /// </param>
    /// <param name="User">
    ///     The username or identifier of the user associated with the operation
    /// </param>
    public record AggrConfigCopy(int Id, string Name, string User);

    /// <summary>
    ///     Represents the unique code identifier for an aggregation configuration
    /// </summary>
    /// <param name="Code">
    ///     The unique alphanumeric code that identifies the aggregation configuration.
    ///     Must be non-null and non-whitespace.
    /// </param>
    /// <remarks>
    ///     This record is used to pass aggregation configuration codes between application layers.
    ///     The code typically follows a specific naming convention defined by the system.
    /// </remarks>
    public record AggrConfigCode(string Code);

    /// <summary>
    ///     Represents an approval action on an aggregation configuration
    /// </summary>
    /// <param name="Id">
    ///     The unique identifier of the aggregation configuration being approved/rejected
    /// </param>
    /// <param name="Username">
    ///     The username of the person performing the approval action
    /// </param>
    /// <param name="Role">
    ///     The role of the user in the approval process (e.g., "Approver", "Reviewer")
    /// </param>
    /// <param name="Action">
    ///     The approval action being taken (e.g., "Approve", "Reject", "Request Changes")
    /// </param>
    /// <param name="Note">
    ///     Optional comments or notes regarding the approval decision
    /// </param>
    /// <remarks>
    ///     This record is used to track and process approval workflow
    ///     for aggregation configuration changes.
    /// </remarks>
    public record AggrConfigApproval(
        int Id,
        string? Username,
        string? Role,
        string? Action,
        string? Note
    );

    /// <summary>
    ///     Retrieves a paginated list of aggregation configurations based on the specified filter type
    /// </summary>
    /// <remarks>
    ///     This endpoint returns a collection of aggregation configurations filtered by the specified grid type.
    ///     The response is formatted as a JSON array containing configuration objects.
    ///
    ///     Sample request:
    ///     ```
    ///     GET /AggrMapping/AggregateConfig/DataGrid/ALL
    ///     GET /AggrMapping/AggregateConfig/DataGrid/LAST_ACTIVE
    ///     GET /AggrMapping/AggregateConfig/DataGrid/LAST_VERSION
    ///     ```
    /// </remarks>
    /// <param name="gridType">
    ///     The type of filter to apply when retrieving configurations.
    ///
    ///     Available values:
    ///     - ALL: Returns all available configurations (default)
    ///     - LAST_ACTIVE: Returns only configurations that are currently active
    ///     - LAST_VERSION: Returns only the most recent version of each configuration
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
    [Tags(tags: "Aggregation Config For FE"), HttpGet("DataGrid/{gridType}")]
    public async Task<APIResponseData<JArray?>> DataGrid(
        [FromRoute] string? gridType = "ALL",
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            gridType.ThrowIfNullOrWhitespace(nameof(gridType));

            return new APIResponseData<JArray?>().ChangeData(
                data: await pgHelper.FE_GetDataGrid(
                    gridType: gridType,
                    cancellationToken: cancellationToken
                ) ?? new JArray()
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
    [Tags(tags: "Aggregation Config For FE"), HttpPost("Detail")]
    public async Task<APIResponseData<JObject?>> Detail(
        AggConfigId payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var detail = await pgHelper.FE_GetDetail(
                id: payload.Id,
                cancellationToken: cancellationToken
            );

            return new APIResponseData<JObject?>().ChangeData(
                data: detail is { Count: > 0 }
                    ? detail
                    : throw new DataException(
                        $"Aggregate configuration with id '{payload.Id}' not found."
                    )
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
    [Tags("Aggregation Config For FE"), HttpPost("AggrConfigIdAndUser/Delete")]
    public async Task<APIResponseData<JObject?>> Delete(
        AggrConfigIdAndUser payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return new APIResponseData<JObject?>().ChangeData(
                data: await pgHelper.FE_Remove(
                    id: payload.Id,
                    username: payload.User,
                    cancellationToken: cancellationToken
                )
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
    [Tags("Aggregation Config For FE"), HttpPost("AggrConfigIdAndUser/Rollback")]
    public async Task<APIResponseData<JObject?>> Rollback(
        AggrConfigIdAndUser payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return new APIResponseData<JObject?>().ChangeData(
                data: await pgHelper.FE_Rollback(
                    id: payload.Id,
                    username: payload.User,
                    cancellationToken: cancellationToken
                )
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
    [Tags("Aggregation Config For FE"), HttpPost("AggrConfigIdAndUser/Log")]
    public async Task<APIResponseData<JObject?>> Log(
        AggrConfigCode payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return new APIResponseData<JObject?>().ChangeData(
                data: await pgHelper.FE_Log(
                    code: payload.Code,
                    cancellationToken: cancellationToken
                )
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
    [Tags("Aggregation Config For FE"), HttpPost("AggrConfigIdAndUser/Copy")]
    public async Task<APIResponseData<JObject?>> Copy(
        AggrConfigCopy payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return new APIResponseData<JObject?>().ChangeData(
                data: await pgHelper.FE_Copy(
                    id: payload.Id,
                    name: payload.Name,
                    username: payload.User,
                    cancellationToken: cancellationToken
                )
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
    ///     POST /AggrMapping/AggregateConfig/AggrConfigIdAndUser/Approval
    ///     {
    ///         "Id": 12345,
    ///         "Username": "approver@example.com",
    ///         "Role": "Senior Reviewer",
    ///         "Action": "Approve",
    ///         "Note": "Configuration meets all requirements"
    ///     }
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
    [Tags("Aggregation Config For FE"), HttpPost("AggrConfigIdAndUser/Approval")]
    public async Task<APIResponseData<JObject?>> Approval(
        AggrConfigApproval payload,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return new APIResponseData<JObject?>().ChangeData(
                data: await pgHelper.FE_Approval(
                    id: payload.Id,
                    username: payload.Username,
                    role: payload.Role,
                    action: payload.Action,
                    note: payload.Note,
                    cancellationToken: cancellationToken
                )
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
