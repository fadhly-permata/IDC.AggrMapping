using IDC.AggrMapping.Utilities.Models.AggregateEngine;
using IDC.Utilities.Data;
using IDC.Utilities.Extensions;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities.Data;

internal static class GlobalConfigurationData
{
    internal static async Task<JObject?> GetGlobalConfigurationsAsync(
        this PostgreHelper pgHelper,
        string[] configCodes,
        CancellationToken cancellationToken = default
    )
    {
        if (configCodes == null || configCodes.Length == 0)
            throw new ArgumentNullException(paramName: nameof(configCodes));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        (_, var result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_global_configs",
                Parameters =
                [
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_config_codes",
                        Value = string.Join(separator: ",", value: configCodes),
                    },
                ],
            },
            callback: data => { },
            cancellationToken: cancellationToken
        );

        return result != null ? JObject.Parse(json: result.ToString() ?? "{}") : null;
    }
}

internal static class InsertEngineData
{
    internal static async Task<JObject?> GetGroupedMappingAsync(
        this PostgreHelper pgHelper,
        string mapCode,
        CancellationToken cancellationToken = default
    )
    {
        mapCode.ThrowIfNullOrWhitespace(nameof(mapCode));

        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        (_, var result) = await pgHelper.ExecuteScalarAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "aggregation",
                SPName = "get_map_configs",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_map_code", Value = mapCode },
                ],
            },
            callback: data => { },
            cancellationToken: cancellationToken
        );

        return result != null ? JObject.Parse(json: result.ToString() ?? "{}") : null;
    }
}

internal static class AggregateData
{
    internal static async Task<AggregateConfigurationModel?> GetAggregateConfigurationAsync(
        this PostgreHelper pgHelper,
        string aggregateCode,
        CancellationToken cancellationToken = default
    )
    {
        await Task.CompletedTask;
        return new AggregateConfigurationModel
        {
            AggregateCode = aggregateCode,
            ConfigurationItems = new JObject
            {
                // 1. Total gaji semua karyawan
                { "total_salary", "SUM(Departments[].Employees[].Salary)" },
                // 2. Rata-rata gaji karyawan aktif
                { "avg_salary_active", "AVG(Departments[].Employees[].Salary~IsActive == true)" },
                // 3. Jumlah karyawan per departemen
                {
                    "employee_count_per_dept",
                    "COUNT(Departments[].Employees[]~ ~Departments[].Name ASC)"
                },
                // 4. Total gaji per departemen
                {
                    "salary_per_dept",
                    "SUM(Departments[].Employees[].Salary~ ~Departments[].Name ASC)"
                },
                // 5. Gaji tertinggi
                { "max_salary", "MAX(Departments[].Employees[].Salary)" },
                // 6. Gaji terendah dari karyawan aktif
                { "min_salary_active", "MIN(Departments[].Employees[].Salary~IsActive == true)" },
                // 7. Jumlah karyawan dengan skill tertentu
                { "count_java_devs", "COUNT(Departments[].Employees[]~Skills CONTAINS 'Java')" },
                // 8. Rata-rata gaji per departemen untuk karyawan aktif
                {
                    "avg_salary_active_per_dept",
                    "AVG(Departments[].Employees[].Salary~IsActive == true~Departments[].Name ASC"
                },
            },
        };
    }
}
