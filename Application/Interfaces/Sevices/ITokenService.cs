using Domain.Entities;
using Domain.HelperClasses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Interfaces;

/// <summary>
/// Service interface for security token management.
/// Responsible for generating, validating, and managing the lifecycle of Access, Refresh, and Reset tokens.
/// </summary>
public interface ITokenService : IScopedService
{
    /// <summary>
    /// Generates a JWT access token containing user claims and roles.
    /// </summary>
    /// <param name="user">The user entity for whom the token is generated.</param>
    /// <returns>A tuple containing the <see cref="JwtSecurityToken"/> object and its string representation.</returns>
    Task<(JwtSecurityToken, string)> GenerateAccessToken(User user);

    /// <summary>
    /// Generates a secure, random refresh token with a configured expiration date.
    /// </summary>
    /// <returns>A <see cref="TokenInfo"/> object containing the token value and expiry date.</returns>
    TokenInfo GenerateRefreshToken();

    /// <summary>
    /// Validates an expired access token and extracts the claims principal.
    /// Used during the token refresh process to identify the user.
    /// </summary>
    /// <param name="token">The expired JWT access token string.</param>
    /// <returns>The <see cref="ClaimsPrincipal"/> extracted from the token if valid; otherwise, null.</returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>
    /// Stores the access and refresh tokens in secure, HttpOnly, and SameSite cookies.
    /// </summary>
    /// <param name="tokenInfo">The access token information.</param>
    /// <param name="refreshInfo">The refresh token information.</param>
    void SetTokenCookies(TokenInfo tokenInfo, TokenInfo refreshInfo);

    /// <summary>
    /// Removes access and refresh token cookies from the client's browser.
    /// </summary>
    void ClearTokenCookies();

    /// <summary>
    /// Generates a specialized token for password reset operations.
    /// </summary>
    /// <param name="user">The user requesting the password reset.</param>
    /// <param name="expiresInMinutes">The validity duration of the reset token in minutes.</param>
    /// <returns>A tuple containing the <see cref="UserToken"/> entity and the string-encoded access token.</returns>
    (UserToken refreshToken, string AccessToken) GenerateResetToken(User user, int expiresInMinutes);
}