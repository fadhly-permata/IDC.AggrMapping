using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

public partial class InsertDataEngine
{
    private async Task LogWritter(JObject data) =>
        await Task.Run(() =>
        {
            // TODO: Tunggu konfirmasi BA untuk metode logging-nya
            systemLogging.LogInformation(data.ToString());
        });

    private async Task<TIn?> DataCaching<TIn>(string type, string name, TIn? data) =>
        await caching.GetOrSetAsync(
            key: CacheTypeAndNameBuilder(type, name),
            valueFactory: () => Task.FromResult(data),
            expirationRenewal: true,
            expirationMinutes: 60
        );

    internal static string CacheTypeAndNameBuilder(string type, string name) => $"{type}.{name}";
}
