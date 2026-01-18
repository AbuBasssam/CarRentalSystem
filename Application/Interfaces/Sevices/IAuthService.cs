using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Domain.HelperClasses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Interfaces;

public interface IAuthService : IScopedService
{
    Task<JwtAuthResult> GetJwtAuthForuser(User User);
    (int, Exception?) GetUserIdFromJwtAccessTokenObj(JwtSecurityToken jwtAccessTokenObj);
    string? GetJtiFromAccessTokenString(string accessToken);
    Result<string> GetEmailFromSessionToken(string sessionToken);
    Result<int> GetUserIdFromSessionToken(string sessionToken);
    bool IsValidAccessToken(string AccessTokenStr);
    (JwtSecurityToken?, Exception?) GetJwtAccessTokenObjFromAccessTokenString(string AccessToken);
    (ClaimsPrincipal?, Exception?) GetClaimsPrinciple(string AccessToken);
    Task<(UserToken?, Exception?)> ValidateRefreshToken(int UserId, string RefreshTokenStr);
    (UserToken refreshToken, string AccessToken) GenerateVerificationToken(User user, int minutesValidDuration);
    (UserToken refreshToken, string AccessToken) GenerateResetToken(User user, int minutesValidDuration);


    /// <summary>
    /// Logout user by revoking their tokens
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jwtId">JWT ID to revoke</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> Logout(int userId, string jwtId);

    /// <summary>
    /// Logout from all devices by revoking all user tokens
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> LogoutFromAllDevices(int userId);
    Task<bool> ValidateSessionToken(string sessionToken, enTokenType tokenType);
    Task<bool> ValidateResetPasswordToken(string token, enResetPasswordStage requiredStage);

}
