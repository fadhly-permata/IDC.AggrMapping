using Newtonsoft.Json.Linq;

namespace IDC.AggrMapping.Utilities;

/// <summary>
/// Commons helper class for common operations
/// </summary>
public static class Commons
{
    /// <summary>
    /// Indicates whether the application is running in debug mode.
    /// </summary>
    internal static bool IS_DEBUG_MODE
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    internal static bool ValidateJsonDepth(this string jsonString, int maxDepth = 4) =>
        ValidateJTokenDepth(token: JToken.Parse(json: jsonString), maxDepth: maxDepth);

    internal static bool ValidateJsonDepth(this JObject jsonObject, int maxDepth = 4) =>
        ValidateJTokenDepth(token: jsonObject, maxDepth: maxDepth);

    internal static bool ValidateJTokenDepth(
        this JToken token,
        int maxDepth = 4,
        int currentDepth = 1
    )
    {
        if (currentDepth > maxDepth)
            return false;

        if (token.Type == JTokenType.Object)
        {
            var invalidProperties = ((JObject)token)
                .Properties()
                .Where(predicate: property =>
                    !ValidateJTokenDepth(
                        token: property.Value,
                        maxDepth: maxDepth,
                        currentDepth: currentDepth + 1
                    )
                );

            return !invalidProperties.Any();
        }
        else if (token.Type == JTokenType.Array)
        {
            var invalidItems = ((JArray)token).Where(predicate: item =>
                !ValidateJTokenDepth(
                    token: item,
                    maxDepth: maxDepth,
                    currentDepth: currentDepth + 1
                )
            );

            return !invalidItems.Any();
        }

        return true;
    }
}
