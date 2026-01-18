namespace Interfaces;

/// <summary>
/// Service for validating authentication tokens against database state
/// </summary>
/// <remarks>
/// This service is called during JWT authentication to verify that tokens
/// haven't been revoked, used, or expired in the database.
/// 
/// Validation checks:
/// - Token exists in database
/// - Token is not revoked (IsRevoked = false)
/// - Token is not used (IsUsed = false)
/// - Token is not expired (ExpiryDate > now)
/// </remarks>
public interface ITokenValidationService : IScopedService
{
    /// <summary>
    /// Validates a token by its JWT ID (JTI) against database state
    /// </summary>
    /// <param name="jti">JWT ID (unique identifier) from the token claims</param>
    /// <returns>
    /// True if token is valid (exists, not revoked, not used, not expired);
    /// False if token is invalid or an error occurs
    /// </returns>
    Task<bool> ValidateTokenAsync(string jti);
}
