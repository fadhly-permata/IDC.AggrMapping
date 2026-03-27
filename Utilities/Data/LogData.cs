using IDC.Utilities.Data;

namespace IDC.AggrMapping.Utilities.Data;

internal enum LogDataProcessType
{
    // ReSharper disable once InconsistentNaming
    ML_AGGREGATE,

    // ReSharper disable once InconsistentNaming
    ML_INSERTDATA,

    // ReSharper disable once InconsistentNaming
    ML_WF_OR_DF,
}

// p_batch_no character varying, p_aggr_code character varying, p_request json DEFAULT NULL::json, p_response json DEFAULT NULL::json, p_log text DEFAULT NULL::text
internal class LogDataModel
{
    public required string BatchCode { get; set; }
    public required LogDataProcessType ProcessType { get; set; }
    public required string ProcessCode { get; set; }
    public string? Request { get; set; }
    public string? Response { get; set; }
    public string? Log { get; set; }
}

internal static class LogData
{
    internal static async Task Save(
        this LogDataModel model,
        PostgreHelper pgHelper,
        CancellationToken cancellationToken = default
    )
    {
        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        await pgHelper.ExecuteNonQueryAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "log_data_proc",
                SPName = "upsert_multilayer_aggregate_proc",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_batch_no", Value = model.BatchCode },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_process_type",
                        Value = model.ProcessType.ToString(),
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_process_code",
                        Value = model.ProcessCode,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_request",
                        Value = model.Request,
                        DataType = NpgsqlTypes.NpgsqlDbType.Json,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_response",
                        Value = model.Response,
                        DataType = NpgsqlTypes.NpgsqlDbType.Json,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_log",
                        Value = model.Log,
                        DataType = NpgsqlTypes.NpgsqlDbType.Text,
                    },
                ],
            },
            cancellationToken: cancellationToken
        );
    }
}
