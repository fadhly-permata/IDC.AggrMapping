using IDC.AggrMapping.Utilities.Models;
using IDC.Utilities;
using IDC.Utilities.Extensions;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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

        var globalConfig = new GlobalConfigurationModel();

        data.Validate(
            configs: new MlaPayloadModel.MlaConfigs(
                MaxMapCount: globalConfig.MaxMapCount,
                MaxDataCount: globalConfig.MaxDataPayload
            )
        );

        return new APIResponseData<object?>().ChangeData(data: data);
    }

    [Tags(tags: "Insert Data"), HttpPost("DeleteData")]
    public APIResponseData<object?> DeleteData([FromBody] JObject data)
    {
        return new APIResponseData<object?>().ChangeData(
            data: data.PropGet<int?>(path: "id", defaultValue: 20)
        );
    }
}
