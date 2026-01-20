namespace Worker.Configuration;

/// <summary>
/// Configuration options for Password Reset Token Cleanup Service
/// Controls the automatic cleanup of expired and old password reset tokens
/// </summary>
public class PasswordResetTokenCleanupOptions
{
    /// <summary>
    /// Enable or disable the cleanup service
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often the cleanup service runs (in hours)
    /// Default: 6 hours (reasonable for password reset tokens)
    /// </summary>
    public int IntervalHours { get; set; } = 6;

    /// <summary>
    /// Token validity period in minutes
    /// After this period, tokens will be auto-revoked
    /// Default: 15 minutes - balanced security approach
    /// </summary>
    public int TokenValidityMinutes { get; set; } = 15;

    /// <summary>
    /// How many days to keep revoked/expired tokens for audit purposes
    /// After this period, tokens will be permanently deleted
    /// Default: 30 days (recommended for security auditing)
    /// </summary>
    public int RetentionDaysAfterExpiry { get; set; } = 30;

    /// <summary>
    /// Maximum number of tokens to process in a single batch
    /// Prevents memory overflow and database lock issues
    /// Default: 500 (smaller than auth tokens due to more frequent runs)
    /// </summary>
    public int BatchSize { get; set; } = 500;

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

        if (TokenValidityMinutes <= 0)
            throw new ArgumentException(
                $"{nameof(TokenValidityMinutes)} must be greater than 0. Current value: {TokenValidityMinutes}");

        if (RetentionDaysAfterExpiry < 0)
            throw new ArgumentException(
                $"{nameof(RetentionDaysAfterExpiry)} cannot be negative. Current value: {RetentionDaysAfterExpiry}");

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

        // Warning validations (not throwing, just logging potential issues)
        if (TokenValidityMinutes > 120)
        {
            // Tokens valid for more than 2 hours might be a security risk
            Console.WriteLine(
                $"WARNING: {nameof(TokenValidityMinutes)} is set to {TokenValidityMinutes} minutes. " +
                "Password reset tokens should typically expire within 1-2 hours for security.");
        }

        if (RetentionDaysAfterExpiry > 90)
        {
            Console.WriteLine(
                $"WARNING: {nameof(RetentionDaysAfterExpiry)} is set to {RetentionDaysAfterExpiry} days. " +
                "Consider reducing retention period to save database space.");
        }

        if (BatchSize > 1000)
        {
            Console.WriteLine(
                $"WARNING: {nameof(BatchSize)} is set to {BatchSize}. " +
                "Large batch sizes may cause database performance issues.");
        }
    }
}