using Application.Models;
using Domain.Entities;
using Domain.HelperClasses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations.
/// Manages JWT generation, token validation, and session handling.
/// </summary>
public interface IAuthService : IScopedService
{
    /// <summary>
    /// Generates a complete JWT authentication result (Access and Refresh tokens) for a user.
    /// </summary>
    Task<JwtAuthResult> GetJwtAuthForuser(User User);

    /// <summary>
    /// Extracts the User ID from a JWT security token object.
    /// </summary>
    (int, Exception?) GetUserIdFromJwtAccessTokenObj(JwtSecurityToken jwtAccessTokenObj);

    /// <summary>
    /// Extracts the JTI (unique identifier) from an access token string.
    /// </summary>
    string? GetJtiFromAccessTokenString(string accessToken);

    /// <summary>
    /// Validates a session token and returns the associated email.
    /// </summary>
    Result<string> GetEmailFromSessionToken(string sessionToken);

    /// <summary>
    /// Validates a session token and returns the associated user ID.
    /// </summary>
    Result<int> GetUserIdFromSessionToken(string sessionToken);

    /// <summary>
    /// Checks if the provided access token string is cryptographically valid.
    /// </summary>
    bool IsValidAccessToken(string AccessTokenStr);

    /// <summary>
    /// Parses an access token string into a JwtSecurityToken object.
    /// </summary>
    (JwtSecurityToken?, Exception?) GetJwtAccessTokenObjFromAccessTokenString(string AccessToken);

    /// <summary>
    /// Retrieves the ClaimsPrincipal from a valid access token.
    /// </summary>
    (ClaimsPrincipal?, Exception?) GetClaimsPrinciple(string AccessToken);

    /// <summary>
    /// Validates a refresh token against the stored version in the database.
    /// </summary>
    Task<(UserToken?, Exception?)> ValidateRefreshToken(int UserId, string refreshToken, string jwtId);

    /// <summary>
    /// Generates a new password reset token for a user with a specific validity duration.
    /// </summary>
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

    /// <summary>
    /// Configures the refresh token in the response cookie.
    /// </summary>
    void SetRefreshTokenCookie(string refreshToken, DateTime refreshTokenExpires);

    /// <summary>
    /// Delete the refresh token from the response cookie.
    /// </summary>
    void DeleteRefreshTokenCookie();

}
