namespace Application.Features.AuthFeature;

/// <summary>
/// Response DTO for token validation endpoint
/// Contains ONLY user information NOT available in JWT claims
/// (Email, UserId, Roles are already in token claims - frontend can extract them)
/// </summary>
public class TokenValidationResponseDTO
{
    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Path to user's profile image (nullable)
    /// Can change frequently, so not stored in JWT
    /// </summary>
    public string? ImagePath { get; set; }
}
