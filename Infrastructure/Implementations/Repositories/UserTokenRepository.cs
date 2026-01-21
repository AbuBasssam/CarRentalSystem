using Domain.Entities;
using Domain.Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserToken entity operations.
/// Manages the lifecycle of security tokens including authentication, refresh, and password reset tokens.
/// </summary>
public class UserTokenRepository : GenericRepository<UserToken, int>, IUserTokenRepository
{
    public UserTokenRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves an active (non-revoked and non-expired) session token for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user owning the token.</param>
    /// <param name="type">The type of token to retrieve (e.g., AuthToken, RefreshToken).</param>
    /// <returns>An IQueryable containing the active session token if found.</returns>
    public IQueryable<UserToken> GetActiveSessionTokenByUserId(int userId, enTokenType type)
    {
        return _dbSet
            .Where(token => token.UserId == userId
                         && !token.IsRevoked
                         && token.ExpiryDate > DateTime.UtcNow
                         && token.Type == type);
    }

    /// <summary>
    /// Retrieves a specific token using its unique JWT ID (JTI).
    /// </summary>
    /// <param name="jti">The unique identifier associated with the JWT.</param>
    /// <returns>An IQueryable containing the matching token record.</returns>
    public IQueryable<UserToken> GetTokenByJti(string jti) => _dbSet.Where(t => t.JwtId == jti);

    /// <summary>
    /// Checks if a token identified by its JTI is expired or already revoked.
    /// </summary>
    /// <param name="jwtId">The JWT ID to check.</param>
    /// <returns>True if the token is expired or revoked; otherwise, false.</returns>
    public async Task<bool> IsTokenExpired(string jwtId)
    {
        var IsTokenExpired = await _dbSet.AnyAsync
            (
                t => t.JwtId!.Equals(jwtId)
                && t.IsRevoked
                && DateTime.UtcNow > t.ExpiryDate
            );

        return IsTokenExpired;
    }

    /// <summary>
    /// Revokes a specific token by its JWT ID and updates its status to 'Used' and 'Expired' immediately.
    /// </summary>
    /// <param name="jwtId">The unique JWT ID to revoke.</param>
    /// <returns>True if the update operation was executed successfully.</returns>
    public async Task<bool> RevokeUserTokenAsync(string jwtId)
    {
        var affectedRows = await _dbSet
                   .Where(token => token.JwtId == jwtId)
                   .ExecuteUpdateAsync(setters => setters
                        .SetProperty(t => t.IsRevoked, true)
                        .SetProperty(t => t.ExpiryDate, DateTime.UtcNow)
                        .SetProperty(t => t.IsUsed, true)
                   );

        return affectedRows >= 0;
    }

    /// <summary>
    /// Revokes all tokens of a specific type for a particular user.
    /// Useful for global logout or clearing specific token categories.
    /// </summary>
    /// <param name="userId">The user ID whose tokens will be revoked.</param>
    /// <param name="type">The category of tokens to revoke.</param>
    /// <returns>True if the update operation was executed successfully.</returns>
    public async Task<bool> RevokeUserTokenAsync(int userId, enTokenType type)
    {
        var affectedRows = await _dbSet
            .Where(token => token.UserId == userId && token.Type == type)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.ExpiryDate, DateTime.UtcNow)
                .SetProperty(t => t.IsUsed, true)
            );

        return affectedRows >= 0;
    }

    /// <summary>
    /// Retrieves expired authentication tokens that have exceeded the retention period for cleanup.
    /// </summary>
    /// <param name="retentionDays">The number of days to keep tokens before they are eligible for deletion.</param>
    /// <param name="batchSize">The maximum number of records to retrieve in one batch.</param>
    /// <returns>A list of expired authentication tokens ready for cleanup.</returns>
    public async Task<List<UserToken>> GetExpiredAuthTokensForCleanupAsync(int retentionDays, int batchSize)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var expiredTokens = await _dbSet
            .Where(t => t.Type == enTokenType.AuthToken && t.ExpiryDate < cutoffDate && (t.IsUsed || t.IsRevoked))
            .OrderBy(t => t.ExpiryDate)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync();

        return expiredTokens;
    }

    /// <summary>
    /// Retrieves password reset tokens that have passed their validity duration but are still marked as active.
    /// Primarily used for automatic revocation of stale reset requests.
    /// </summary>
    /// <param name="validityMinutes">The duration in minutes for which a reset token remains valid.</param>
    /// <param name="batchSize">The maximum number of records to retrieve in one batch.</param>
    /// <returns>A list of reset tokens that should be revoked.</returns>
    public async Task<List<UserToken>> GetExpiredPasswordResetTokensAsync(int validityMinutes, int batchSize)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-validityMinutes);

        return await _dbSet
            .Where(t => t.Type == enTokenType.ResetPasswordToken && !t.IsRevoked && t.CreatedAt <= cutoffTime)
            .OrderBy(t => t.CreatedAt)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves old password reset tokens (revoked, used, or expired) that have exceeded the retention period for permanent deletion.
    /// </summary>
    /// <param name="retentionDays">The number of days to keep historical reset records for audit purposes.</param>
    /// <param name="batchSize">The maximum number of records to retrieve in one batch.</param>
    /// <returns>A list of old reset tokens ready for permanent deletion.</returns>
    public async Task<List<UserToken>> GetOldPasswordResetTokensAsync(int retentionDays, int batchSize)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        return await _dbSet
            .Where(t =>
                t.Type == enTokenType.ResetPasswordToken &&
                t.CreatedAt <= cutoffDate &&
                (t.IsRevoked || t.IsUsed || t.ExpiryDate <= DateTime.UtcNow)
                )
            .OrderBy(t => t.CreatedAt)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
