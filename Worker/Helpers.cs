namespace Worker;
public static class Helpers
{
    /// <summary>
    /// Calculates the delay until the next execution run.
    /// If a specific time (runAt) is provided, it calculates the duration until that time.
    /// Otherwise, it falls back to the provided default interval.
    /// </summary>
    /// <param name="runAt">A string representing the time of day (e.g., "02:00").</param>
    /// <param name="defaultInterval">The fallback interval if runAt is not set or invalid.</param>
    /// <returns>A TimeSpan representing the delay.</returns>
    public static TimeSpan CalculateNextRunDelay(string? runAt, TimeSpan defaultInterval)
    {
        // If no specific run time is configured, use the interval
        if (string.IsNullOrEmpty(runAt))
        {
            return defaultInterval;
        }

        try
        {
            var now = DateTime.UtcNow;
            var scheduledTime = TimeOnly.Parse(runAt);
            var todayScheduled = now.Date.Add(scheduledTime.ToTimeSpan());

            // If today's scheduled time has passed, schedule for tomorrow

            var nextRun = todayScheduled > now
                ? todayScheduled
                : todayScheduled.AddDays(1);

            var delay = nextRun - now;

            // Ensure the delay is positive; otherwise, fallback to interval
            return delay > TimeSpan.Zero ? delay : defaultInterval;
        }
        catch
        {
            // Fallback to default interval if parsing fails
            return defaultInterval;
        }
    }
}
