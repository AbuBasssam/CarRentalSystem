namespace Worker.Configuration;

/// <summary>
/// Configuration options for Unverified User Cleanup Service
/// Controls the automatic deletion of users who haven't verified their email
/// within the allowed time period
/// </summary>
public class UnverifiedUserCleanupOptions
{
    /// <summary>
    /// Enable or disable the cleanup service
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often the cleanup service runs (in hours)
    /// Default: 12 hours (twice daily is optimal for user cleanup)
    /// </summary>
    public int IntervalHours { get; set; } = 12;

    /// <summary>
    /// Time window (in hours) given to users to verify their email
    /// After this period, unverified accounts will be deleted
    /// Default: 24 hours (industry standard)
    /// </summary>
    public int UnverifiedRetentionHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of users to delete in a single batch
    /// Prevents database lock issues and memory overflow
    /// Default: 100 (users are heavier entities than tokens)
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Specific time to run the cleanup (UTC, 24-hour format: "HH:mm")
    /// If not set, uses IntervalHours instead
    /// Example: "02:00" runs at 2 AM UTC daily
    /// Default: null (uses interval-based scheduling)
    /// </summary>
    public string? RunAt { get; set; }

    /// <summary>
    /// Validates the configuration on startup
    /// Throws ArgumentException if any setting is invalid
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid</exception>
    public void Validate()
    {
        if (IntervalHours <= 0)
            throw new ArgumentException(
                $"{nameof(IntervalHours)} must be greater than 0. Current value: {IntervalHours}");

        if (UnverifiedRetentionHours <= 0)
            throw new ArgumentException(
                $"{nameof(UnverifiedRetentionHours)} must be greater than 0. Current value: {UnverifiedRetentionHours}");

        if (BatchSize <= 0)
            throw new ArgumentException(
                $"{nameof(BatchSize)} must be greater than 0. Current value: {BatchSize}");

        // Validate RunAt format if provided
        if (!string.IsNullOrEmpty(RunAt))
        {
            if (!TimeOnly.TryParse(RunAt, out _))
                throw new ArgumentException(
                    $"{nameof(RunAt)} must be in 24-hour format (HH:mm). Current value: {RunAt}");
        }

        // Warning validations
        if (UnverifiedRetentionHours < 12)
        {
            Console.WriteLine(
                $"WARNING: {nameof(UnverifiedRetentionHours)} is set to {UnverifiedRetentionHours} hours. " +
                "Users might not have enough time to verify their email. Recommended: 24-48 hours.");
        }

        if (UnverifiedRetentionHours > 168) // 7 days
        {
            Console.WriteLine(
                $"WARNING: {nameof(UnverifiedRetentionHours)} is set to {UnverifiedRetentionHours} hours. " +
                "Usernames/emails will be locked for a long time. Recommended: 24-72 hours.");
        }

        if (BatchSize > 500)
        {
            Console.WriteLine(
                $"WARNING: {nameof(BatchSize)} is set to {BatchSize}. " +
                "Large batch sizes may cause database performance issues with cascade deletes.");
        }
    }
}