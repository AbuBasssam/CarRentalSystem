namespace Domain.HelperClasses
{
    public class JwtAuthResult
    {
        public string AccessToken { get; set; } = null!;

        public RefreshToken RefreshToken { get; set; } = null!;

    }
}


