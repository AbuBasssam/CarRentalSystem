namespace Domain.HelperClasses;

public class EmailSettings
{
    public int Port { get; set; }
    public string Host { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string Password { get; set; } = null!;
}
