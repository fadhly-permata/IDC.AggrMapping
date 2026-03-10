using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Models;
using IDC.AggrMapping.Utilities.Models.Postgre;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace IDC.AggrMapping.Controllers;

/// <summary>
/// Controller for handling multi-layer adapter operations
/// </summary>
[Route("AggrMapping/[controller]")]
[ApiController]
public partial class InsertDataEngine(
    Caching caching,
    PostgreHelper pgHelper,
    SystemLogging systemLogging
) : ControllerBase
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    ///     Inserts data into the database
    /// </summary>
    /// <param name="data">
    ///     The data to be inserted
    /// </param>
    /// <param name="cancellationToken">
    ///     Cancellation token
    /// </param>
    /// <returns>
    ///     APIResponseData containing the result
    /// </returns>
    [Tags(tags: "Insert Data"), HttpPost("InsertData")]
    public async Task<APIResponseData<object?>> InsertData(
        [FromBody] MlaPayloadModel data,
        CancellationToken cancellationToken = default
    )
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            var globalConfig = await caching.GetOrSetAsync(
                key: "GlobalConfigurations",
                valueFactory: async () =>
                    await new GlobalConfigurationModel().InitFromDatabase(
                        pgHelper: pgHelper,
                        caching: caching,
                        cancellationToken: cancellationToken
                    ),
                expirationRenewal: true,
                expirationMinutes: 60
            );

            data.Validate(configs: globalConfig);

            foreach (var mapCode in data.ConfMaptable)
            {
                var groupMapModel = await new GroupedMappingModel().Load(
                    systemLogging: systemLogging,
                    pgHelper: pgHelper,
                    caching: caching,
                    mapCode: mapCode,
                    cancellationToken: cancellationToken
                );

                if (groupMapModel is not null)
                    await groupMapModel.DoUpsert(
                        payload: data,
                        pgHelper: pgHelper,
                        caching: caching,
                        systemLogging: systemLogging,
                        cancellationToken: cancellationToken
                    );
            }

            return new APIResponseData<object?>().ChangeData(data: data);
        }
        catch (Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
