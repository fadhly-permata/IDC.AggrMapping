using IDC.Utilities;
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

    internal record GlobalConfigurations(
        int MaxParallelProcess,
        int MaxDepth,
        int MaxMapCount,
        int MaxDataPayload
    );

    internal static async Task<GlobalConfigurations> GetGlobalConfigurations(Caching caching) =>
        new(
            MaxParallelProcess: await caching.GetOrSetAsync(
                key: CacheTypeAndNameBuilder("Configs", "MaxParallelProcess"),
                // TODO: Replace with real logic
                valueFactory: () => Task.FromResult(20),
                expirationRenewal: true,
                expirationMinutes: 60
            ),
            MaxDepth: await caching.GetOrSetAsync(
                key: CacheTypeAndNameBuilder("Configs", "MaxDepth"),
                // TODO: Replace with real logic
                valueFactory: () => Task.FromResult(3),
                expirationRenewal: true,
                expirationMinutes: 60
            ),
            MaxMapCount: await caching.GetOrSetAsync(
                key: CacheTypeAndNameBuilder("Configs", "MapCount"),
                // TODO: Replace with real logic
                valueFactory: () => Task.FromResult(5),
                expirationRenewal: true,
                expirationMinutes: 60
            ),
            MaxDataPayload: await caching.GetOrSetAsync(
                key: CacheTypeAndNameBuilder("Configs", "MaxDataPayload"),
                // TODO: Replace with real logic
                valueFactory: () => Task.FromResult(20),
                expirationRenewal: true,
                expirationMinutes: 60
            )
        );
}
