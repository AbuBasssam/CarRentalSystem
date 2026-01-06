using Domain.Entities;
using Domain.Enums;

namespace Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<UserToken, int>
{
    IQueryable<UserToken> GetActiveSessionTokenByUserId(int userId, enTokenType type);
    Task<bool> IsTokenExpired(string jwtId);
    Task<bool> RevokeUserTokenAsync(int userId, enTokenType type);
    Task<bool> RevokeUserTokenAsync(string jwtId);

}
