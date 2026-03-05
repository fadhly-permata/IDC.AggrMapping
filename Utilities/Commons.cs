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

    /// <summary>
    /// Returns a string representation of the duration between two timestamps.
    /// </summary>
    /// <param name="startTime">The start timestamp.</param>
    /// <param name="endTime">The end timestamp.</param>
    /// <returns>A string describing the duration between the two timestamps, in the format "X days Y hours Z minutes W.N seconds".</returns>
    /// <remarks>
    /// If a duration component is zero, it will be omitted from the result string.
    /// </remarks>
    public static string GetDurationFromTimestamp(DateTime startTime, DateTime endTime)
    {
        var timeSpan = endTime - startTime;

        int days = timeSpan.Days;
        int hours = timeSpan.Hours;
        int minutes = timeSpan.Minutes;
        double seconds = Math.Round(
            value: timeSpan.Seconds + (timeSpan.Milliseconds / 1000.0),
            digits: 2
        );

        var durationParts = new List<string>();

        if (days != 0)
            durationParts.Add(item: $"{days} days");

        if (hours != 0)
            durationParts.Add(item: $"{hours} hours");

        if (minutes != 0)
            durationParts.Add(item: $"{minutes} minutes");

        if (Math.Abs(value: seconds) > 0.001)
            durationParts.Add($"{seconds} seconds");

        return string.Join(separator: " ", values: durationParts);
    }
}
