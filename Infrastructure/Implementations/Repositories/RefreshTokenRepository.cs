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

    public async Task<bool> RevokeUserTokenAsync(int userId, enTokenType type)
    {
        var affectedRows = await _dbSet.Where(token => token.UserId == userId && token.Type == type)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(t => t.IsRevoked, true)
            .SetProperty(t => t.ExpiryDate, DateTime.UtcNow));

        return affectedRows >= 0;

    }
}
