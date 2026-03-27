using IDC.Utilities.Data;

namespace IDC.AggrMapping.Utilities.Models;

// p_batch_no character varying, p_aggr_code character varying, p_request json DEFAULT NULL::json, p_response json DEFAULT NULL::json, p_log text DEFAULT NULL::text
internal class LogDataModel
{
    public enum ProcessKind
    {
        // ReSharper disable once InconsistentNaming
        ML_AGGREGATE,

        // ReSharper disable once InconsistentNaming
        ML_INSERTDATA,

        // ReSharper disable once InconsistentNaming
        ML_WF_OR_DF,
    }

    public required string BatchCode { get; set; }
    public required ProcessKind ProcessType { get; set; }
    public required string ProcessCode { get; set; }
    public string? Request { get; set; }
    public string? Response { get; set; }
    public string? Log { get; set; }

    internal async Task Save(PostgreHelper pgHelper, CancellationToken cancellationToken = default)
    {
        await pgHelper.ConnectAsync(cancellationToken: cancellationToken);
        await pgHelper.ExecuteNonQueryAsync(
            spCallInfo: new PostgreHelper.SPCallInfo
            {
                Schema = "log_data_proc",
                SPName = "upsert_multilayer_aggregate_proc",
                Parameters =
                [
                    new PostgreHelper.SPParameter { Name = "p_batch_no", Value = BatchCode },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_process_type",
                        Value = ProcessType.ToString(),
                    },
                    new PostgreHelper.SPParameter { Name = "p_process_code", Value = ProcessCode },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_request",
                        Value = Request,
                        DataType = NpgsqlTypes.NpgsqlDbType.Json,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_response",
                        Value = Response,
                        DataType = NpgsqlTypes.NpgsqlDbType.Json,
                    },
                    new PostgreHelper.SPParameter
                    {
                        Name = "p_log",
                        Value = Log,
                        DataType = NpgsqlTypes.NpgsqlDbType.Text,
                    },
                ],
            },
            cancellationToken: cancellationToken
        );
    }
}
