namespace Worker.Configuration;

/// <summary>
/// Configuration options for OTP Cleanup Background Service
/// </summary>
public class OtpCleanupOptions
{

    /// <summary>
    /// Enable or disable the cleanup service
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval in minutes between each cleanup execution
    /// Default: 15 minutes (OTP is short-lived)
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Number of hours to retain used/expired OTPs before deletion
    /// Default: 1 hour (for audit purposes)
    /// </summary>
    public int RetentionHoursAfterExpiry { get; set; } = 1;

    /// <summary>
    /// Maximum age in hours for any OTP (fallback safety)
    /// Any OTP older than this will be deleted regardless of status
    /// Default: 24 hours
    /// </summary>
    public int MaxAgeHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of OTPs to delete in a single batch operation
    /// Default: 500 (OTPs are smaller than tokens)
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Optional: Specific time to run the cleanup (HH:mm format, 24-hour)
    /// Example: "03:00" for 3 AM
    /// Leave null to run based on IntervalMinutes only
    /// </summary>
    public string? RunAt { get; set; }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (IntervalMinutes <= 0)
            throw new ArgumentException("IntervalMinutes must be greater than 0", nameof(IntervalMinutes));

        if (RetentionHoursAfterExpiry < 0)
            throw new ArgumentException("RetentionHoursAfterExpiry cannot be negative", nameof(RetentionHoursAfterExpiry));

        if (MaxAgeHours <= 0)
            throw new ArgumentException("MaxAgeHours must be greater than 0", nameof(MaxAgeHours));

        if (BatchSize <= 0)
            throw new ArgumentException("BatchSize must be greater than 0", nameof(BatchSize));

        if (!string.IsNullOrEmpty(RunAt) && !TimeOnly.TryParse(RunAt, out _))
            throw new ArgumentException("RunAt must be in HH:mm format (e.g., '03:00')", nameof(RunAt));
    }
}