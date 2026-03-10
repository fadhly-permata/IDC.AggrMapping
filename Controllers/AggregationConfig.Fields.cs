using System;
using IDC.AggrMapping.Utilities.Data;

namespace IDC.AggrMapping.Controllers;

public partial class AggregationConfig
{
    private readonly PostgreSql dataPostgre = new(pgHelper: pgHelper);
}
