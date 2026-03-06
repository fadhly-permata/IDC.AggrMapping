using IDC.AggrMapping.Utilities;
using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Controllers;

public partial class InsertDataEngine
{
    // Validasi Max Depth
    private static void ThrowIfExceedMaxDepth(JObject data, int maxDepth)
    {
        if (!data.ValidateJTokenDepth(maxDepth: maxDepth))
            throw new InvalidOperationException(
                message: $"Cannot process more than {maxDepth} depth."
            );
    }

    private static void ThrowIfExceedMaxDepth(JArray data, int maxDepth)
    {
        var firstInvalidItem = data.FirstOrDefault(item =>
            !item.ValidateJTokenDepth(maxDepth: maxDepth)
        );

        if (firstInvalidItem != null)
            throw new InvalidOperationException(
                message: $"Cannot process more than {maxDepth} depth. Found invalid item."
            );
    }
}
