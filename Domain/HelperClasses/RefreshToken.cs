namespace Domain.HelperClasses;

public class RefreshToken
{
    public string Username { get; set; } = null!;
    public string Value { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
