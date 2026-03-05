using System.Linq.Expressions;
using Hangfire;
using Hangfire.States;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

/// <summary>
///     Controller for managing PostgreSQL database operations
/// </summary>
/// <param name="systemLogging">
///     Service for system logging operations
/// </param>
/// <param name="pgHelper">
///     Service for handling PostgreSQL database operations
/// </param>
/// <param name="language">
///     Service for handling language and localization
/// </param>
[Route("AggrMapping/demo/[controller]")]
[ApiController]
public class DemoHangfire(SystemLogging systemLogging, PostgreHelper pgHelper, Language language)
    : ControllerBase
{
    /// <summary>
    ///     Controller for managing PostgreSQL database operations
    /// </summary>
    /// <param name="query">
    ///     SQL query to be executed
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token for asynchronous operations
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result of the query
    /// </returns>
    [HttpPost(template: "QueryTester")]
    public async Task<APIResponseData<object?>> QueryTester(
        [FromBody] string query,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
            var (_, data) = await pgHelper.ExecuteQueryAsync(
                query: query,
                callback: (Action<List<JObject>>?)null,
                cancellationToken: cancellationToken
            );
            return new APIResponseData<object?>().ChangeData(data: data);
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(exception: ex, logging: systemLogging, includeStackTrace: true);
        }
    }

    [HttpPost(template: "Queue/EnqueueDataProcessing")]
    public async Task<APIResponse> EnqueueDataProcessing([FromBody] string dataId)
    {
        try
        {
            DataProcessingJob.EnqueueDataProcessing(dataId);
            return new APIResponse();
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(exception: ex, logging: systemLogging, includeStackTrace: true);
        }
    }

    [HttpPost(template: "Queue/ScheduleDataProcessing")]
    public async Task<APIResponse> ScheduleDataProcessing([FromBody] string dataId, int delayInSecond)
    {
        try
        {
            DataProcessingJob.ScheduleDataProcessing(dataId, TimeSpan.FromSeconds(delayInSecond));
            return new APIResponse();
        }
        catch (Exception ex)
        {
            return new APIResponse()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(exception: ex, logging: systemLogging, includeStackTrace: true);
        }
    }
}

/// <summary>
/// Service for processing data jobs using Hangfire
/// </summary>
public class DataProcessingJob(SystemLogging systemLogging)
{
    /// <summary>
    /// Processes data asynchronously
    /// </summary>
    /// <param name="dataId">Identifier for the data to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task ProcessDataAsync(string dataId, CancellationToken cancellationToken = default)
    {
        systemLogging.LogInformation($"Starting data processing for ID: {dataId}");

        // LAKUKAN SESUATU DI SINI

        systemLogging.LogInformation($"Completed data processing for ID: {dataId}");
    }

    /// <summary>
    /// Schedules a data processing job
    /// </summary>
    /// <param name="dataId">Identifier for the data to process</param>
    /// <param name="delay">Delay before processing</param>
    public static void ScheduleDataProcessing(string dataId, TimeSpan delay)
    {
        BackgroundJob.Schedule<DataProcessingJob>(
            job => job.ProcessDataAsync(dataId, CancellationToken.None),
            delay
        );
    }

    /// <summary>
    /// Enqueues a data processing job immediately
    /// </summary>
    /// <param name="dataId">Identifier for the data to process</param>
    public static void EnqueueDataProcessing(string dataId)
    {
        BackgroundJob.Enqueue<DataProcessingJob>(job =>
            job.ProcessDataAsync(dataId, CancellationToken.None)
        );
    }
}
