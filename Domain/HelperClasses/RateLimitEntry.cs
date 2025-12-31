namespace Domain.HelperClasses;

/// <summary>
/// A simple model to store rate limit information.
/// </summary>
public class RateLimitEntry
{
    public int Count { get; set; }
    public DateTime ExpiresAt { get; set; }
}