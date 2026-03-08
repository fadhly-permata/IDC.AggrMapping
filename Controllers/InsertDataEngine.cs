using IDC.AggrMapping.Utilities;
using IDC.AggrMapping.Utilities.Data;
using IDC.AggrMapping.Utilities.Models;
using IDC.AggrMapping.Utilities.Models.Postgre;
using IDC.Utilities;
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
    SystemLogging systemLogging
// PostgreHelper pgHelper
) : ControllerBase
{
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
        try
        {
            // TODO: uncomment code di bawah ini dan hapus pragma-nya
#pragma warning disable S125
            // var globalConfig = await caching.GetOrSetAsync(
            //     key: "GlobalConfigurations",
            //     valueFactory: () =>
            //         new GlobalConfigurations().InitFromDatabase(
            //             pgHelper: pgHelper,
            //             cancellationToken: cancellationToken
            //         ),
            //     expirationRenewal: true,
            //     expirationMinutes: 60
            // );
#pragma warning restore S125

            var globalConfig = new GlobalConfigurationModel();

            data.Validate(configs: globalConfig);

            foreach (var mapCode in data.ConfMaptable)
            {
                var groupMapModel = await new GroupedMappingModel().Load(
                    systemLogging: systemLogging,
                    // pgHelper: pgHelper,
                    caching: caching,
                    mapCode: mapCode,
                    cancellationToken: cancellationToken
                );

                await groupMapModel.ProcessMapToDB(
                    payloadData: data,
                    // pgHelper: pgHelper,
                    cancellationToken: cancellationToken
                );
            }

            return new APIResponseData<object?>().ChangeData(data: data);
        }
        catch (System.Exception ex)
        {
            return new APIResponseData<object?>()
                .ChangeStatus(status: "Failed")
                .ChangeMessage(
                    exception: ex,
                    logging: systemLogging,
                    includeStackTrace: Commons.IS_DEBUG_MODE
                );
        }
    }
}
