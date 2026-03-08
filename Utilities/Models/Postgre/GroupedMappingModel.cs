using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using IDC.AggrMapping.Utilities.Data;
using IDC.Utilities;
using IDC.Utilities.Interfaces;
using Newtonsoft.Json;

namespace IDC.AggrMapping.Utilities.Models.Postgre;

/// <summary>
/// Represents a grouped mapping configuration for data transformation operations.
/// </summary>
public class GroupedMappingModel : BaseModel<GroupedMappingModel>
{
    /// <summary>
    /// Gets or sets the collection of grouped mappings.
    /// </summary>
    [
        JsonProperty(propertyName: "grouped_mappings"),
        JsonPropertyName(name: "grouped_mappings"),
        Required,
        MinLength(length: 1)
    ]
    public List<MappingGroup> GroupedMappings { get; set; } = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="GroupedMappingModel"/> class.
    /// </summary>
    /// <param name="systemLogging">
    ///     The system logging instance.
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
    public async Task<GroupedMappingModel?> Load(
        SystemLogging systemLogging,
        // PostgreHelper pgHelper,
        Caching caching,
        string mapCode,
        CancellationToken cancellationToken = default
    )
    {
        var result = await caching.GetOrSetAsync(
            key: $"GroupedMappingModel-{mapCode}",
            valueFactory: async () =>
                await FetchFromDB(
                    systemLogging: systemLogging,
                    // pgHelper: pgHelper,
                    cancellationToken: cancellationToken
                ),
            expirationRenewal: true,
            expirationMinutes: 60
        );

        if (result is null)
            await this.WriteFailLog(
                mapCode: mapCode,
                systemLogging: systemLogging,
                cancellationToken: cancellationToken
            );
        else
            await this.WriteSuccessLog(
                mapCode: mapCode,
                systemLogging: systemLogging,
                cancellationToken: cancellationToken
            );

        return result;
    }

    // TODO: Ganti kode dummy ini ke load data dari DB dan hapus pragma-nya
#pragma warning disable S1172
    private async Task<GroupedMappingModel> FetchFromDB(
        SystemLogging systemLogging,
        // PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
#pragma warning restore S1172
    {
        await Task.CompletedTask;

        var strJson =
            @"{
                ""grouped_mappings"": [
                    {
                        ""destination"": {
                            ""db_type"": ""POSTGRES"",
                            ""db_name"": ""customer_db"",
                            ""schema"": ""public"",
                            ""table"": ""customer_profile""
                        },
                        ""operation"": ""UPSERT"",
                        ""field_mappings"": [
                            {
                                ""source_field"": ""cst_ktp"",
                                ""destination_column"": ""ktp""
                            },
                            {
                                ""source_field"": ""cst_email"",
                                ""destination_column"": ""email"",
                            },
                            {
                                ""source_field"": ""cst_birthdate"",
                                ""destination_column"": ""birth_date"",
                            },
                            {
                                ""source_field"": ""cst_fullname"",
                                ""destination_column"": ""full_name"",
                            }
                        ]
                    }
                ]
            }";

        SetLogger(logger: systemLogging);
        MapProperties(json: strJson);

        return this;
    }
}
