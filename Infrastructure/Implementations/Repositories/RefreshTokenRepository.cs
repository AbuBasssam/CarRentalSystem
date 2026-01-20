using Domain.Entities;
using Domain.Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository : GenericRepository<UserToken, int>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }
    public IQueryable<UserToken> GetActiveSessionTokenByUserId(int userId, enTokenType type)
    {
        return _dbSet
            .Where(token => token.UserId == userId
                         && !token.IsRevoked
                         && token.ExpiryDate > DateTime.UtcNow
                         && token.Type == type);
    }

    public IQueryable<UserToken> GetTokenByJti(string jti) => _dbSet.Where(t => t.JwtId == jti);

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
    /// Gets expired auth tokens that have exceeded the retention period for cleanup
    /// </summary>
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
}
