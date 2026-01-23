namespace Domain.HelperClasses;

public class TokenInfo
{
    public string Value { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}