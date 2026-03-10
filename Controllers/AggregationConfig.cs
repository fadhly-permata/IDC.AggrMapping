using IDC.AggrMapping.Utilities;
using IDC.Utilities;
using IDC.Utilities.Data;
using IDC.Utilities.Models.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class AggregationConfig(
        SystemLogging systemLogging,
        PostgreHelper pgHelper) : ControllerBase
    {

        [Tags(tags: "List Data"), HttpGet("GetLastVersion")]
        public async Task<APIResponseData<object?>> GetLastVersion(
        CancellationToken cancellationToken = default)
        {
            try
            {
                return new APIResponseData<object?>().ChangeData(data: await dataPostgre.GetAllDataVersion());
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

        }

        [Tags(tags: "List Data"), HttpGet("GetActiveData")]
        public async Task<APIResponseData<object?>> GetActiveData(
        CancellationToken cancellationToken = default)
        {

            try
            {
                return new APIResponseData<object?>().ChangeData(data: await dataPostgre.GetAllDataActive());
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
        }
    }
}
