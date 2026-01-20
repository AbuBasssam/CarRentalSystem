using Domain.Entities;
using Domain.Enums;

namespace Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<UserToken, int>
{
    IQueryable<UserToken> GetActiveSessionTokenByUserId(int userId, enTokenType type);
    IQueryable<UserToken> GetTokenByJti(string jti);
    Task<bool> IsTokenExpired(string jwtId);
    Task<bool> RevokeUserTokenAsync(int userId, enTokenType type);
    Task<bool> RevokeUserTokenAsync(string jwtId);

    /// <summary>
    /// Gets expired auth tokens that have exceeded the retention period for cleanup
    /// Returns tokens where:
    /// - Type = AuthToken
    /// - Token is expired 
    /// - Retention period has passed: (Now - ExpiryDate) > retentionDays
    /// </summary>
    /// <param name="retentionDays">Number of days to keep expired tokens for audit purposes</param>
    /// <param name="batchSize">Maximum number of tokens to return in one batch</param>
    /// <returns>List of expired tokens ready for deletion</returns>
    Task<List<UserToken>> GetExpiredAuthTokensForCleanupAsync(int retentionDays, int batchSize);

    /// <summary>
    /// Gets password reset tokens that have exceeded their validity period but are not yet revoked
    /// Returns tokens where:
    /// - Type = ResetPasswordToken
    /// - Token is NOT revoked (IsRevoked = false)
    /// - Token has exceeded validity: (Now - CreatedAt) > validityMinutes
    /// </summary>
    /// <param name="validityMinutes">Token validity period in minutes (e.g., 60 for 1 hour)</param>
    /// <param name="batchSize">Maximum number of tokens to return in one batch</param>
    /// <returns>List of expired tokens ready for revocation</returns>
    Task<List<UserToken>> GetExpiredPasswordResetTokensAsync(int validityMinutes, int batchSize);

    /// <summary>
    /// Gets old password reset tokens that have exceeded the retention period for permanent deletion
    /// Returns tokens where:
    /// - Type = ResetPasswordToken
    /// - Token is revoked OR used OR expired
    /// - Retention period has passed: (Now - CreatedAt) > retentionDays
    /// </summary>
    /// <param name="retentionDays">Number of days to keep old tokens for audit purposes</param>
    /// <param name="batchSize">Maximum number of tokens to return in one batch</param>
    /// <returns>List of old tokens ready for permanent deletion</returns>
    Task<List<UserToken>> GetOldPasswordResetTokensAsync(int retentionDays, int batchSize);

}
