namespace Worker.Configuration;

/// <summary>
/// Configuration options for Auth Token Cleanup Background Service
/// </summary>
public class AuthTokenCleanupOptions
{

    /// <summary>
    /// Enable or disable the cleanup service
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval in hours between each cleanup execution
    /// Default: 24 hours (once daily)
    /// </summary>
    public int IntervalHours { get; set; } = 24;

    /// <summary>
    /// Number of days to retain expired tokens before permanent deletion
    /// Default: 7 days (for audit purposes)
    /// </summary>
    public int RetentionDaysAfterExpiry { get; set; } = 7;

    /// <summary>
    /// Maximum number of tokens to delete in a single batch operation
    /// Default: 1000 (prevents long-running transactions)
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Optional: Specific time to run the cleanup (HH:mm format, 24-hour)
    /// Example: "03:00" for 3 AM
    /// Leave null to run based on IntervalHours only
    /// </summary>
    public string? RunAt { get; set; }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (IntervalHours <= 0)
            throw new ArgumentException("IntervalHours must be greater than 0", nameof(IntervalHours));

        if (RetentionDaysAfterExpiry < 0)
            throw new ArgumentException("RetentionDaysAfterExpiry cannot be negative", nameof(RetentionDaysAfterExpiry));

        if (BatchSize <= 0)
            throw new ArgumentException("BatchSize must be greater than 0", nameof(BatchSize));

        if (!string.IsNullOrEmpty(RunAt) && !TimeOnly.TryParse(RunAt, out _))
            throw new ArgumentException("RunAt must be in HH:mm format (e.g., '03:00')", nameof(RunAt));
    }
}
