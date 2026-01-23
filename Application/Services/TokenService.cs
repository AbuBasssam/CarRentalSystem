using Domain.AppMetaData;
using Domain.Entities;
using Domain.Enums;
using Domain.HelperClasses;
using Domain.Security;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Application.Services;

/// <summary>
/// Implementation of <see cref="ITokenService"/> using JWT (JSON Web Tokens) 
/// and secure Cookies for authentication and authorization.
/// </summary>
public class TokenService : ITokenService
{
    #region Field(s)
    private readonly JwtSettings _jwtSettings;
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;


    private readonly SymmetricSecurityKey _signaturekey;
    private static string _SecurityAlgorithm = SecurityAlgorithms.HmacSha256Signature;
    private static JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

    private const string Access_Token_Key = "accessToken";
    private const string Refresh_Token_Key = "refreshToken";

    #endregion

    #region Constructor(s)
    public TokenService(JwtSettings jwtSettings, IUserService userService, IHttpContextAccessor httpContextAccessor)
    {
        _jwtSettings = jwtSettings;
        _userService = userService;

        _httpContextAccessor = httpContextAccessor;

        _signaturekey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret!));
    }
    #endregion

    /// <summary>
    /// Generates a new JWT access token for the specified user, including their assigned roles as claims.
    /// </summary>
    /// <param name="user">The user entity for whom the token is generated.</param>
    /// <returns>A tuple containing the <see cref="JwtSecurityToken"/> object and its serialized string value.</returns>
    public async Task<(JwtSecurityToken, string)> GenerateAccessToken(User user)
    {
        List<string> roles = await _userService.GetUserRolesAsync(user);

        var claims = _GenerateUserClaims(user, roles);

        JwtSecurityToken Obj = _GetJwtSecurityToken(claims);

        var Value = new JwtSecurityTokenHandler().WriteToken(Obj);

        return (Obj, Value);

    }


    /// <summary>
    /// Generates a cryptographically strong random string to serve as a refresh token with a pre-configured expiration period.
    /// </summary>
    /// <returns>A <see cref="TokenInfo"/> object containing the token value and its expiration date.</returns>
    public TokenInfo GenerateRefreshToken()
    {
        return new TokenInfo()
        {
            Value = Helpers.GenerateRandomString64Length(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDate)
        };
    }


    /// <summary>
    /// Extracts and validates the <see cref="ClaimsPrincipal"/> from an expired JWT token.
    /// This is used during the refresh process to identify the user while ignoring the token's expiration date.
    /// </summary>
    /// <param name="token">The expired access token string.</param>
    /// <returns>The principal containing user claims if the signature is valid; otherwise, null.</returns>
    /// <exception cref="SecurityTokenException">Thrown when the token signature or security algorithm is invalid.</exception>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {


        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = _signaturekey,
            ClockSkew = TimeSpan.Zero
        };


        try
        {
            var principal = _tokenHandler.ValidateToken(
                token,
                tokenValidationParameters,
                out SecurityToken securityToken
            );

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to validate expired token");
            return null;
        }
    }


    /// <summary>
    /// Stores the access and refresh tokens in secure, HttpOnly cookies within the HTTP response.
    /// The refresh token cookie is restricted to the refresh path for enhanced security.
    /// </summary>
    /// <param name="tokenInfo">Information about the generated access token.</param>
    /// <param name="refreshInfo">Information about the generated refresh token.</param>
    public void SetTokenCookies(TokenInfo tokenInfo, TokenInfo refreshInfo)
    {

        // Access Value Cookie
        _httpContextAccessor?.HttpContext?.Response.Cookies.Append(
            Access_Token_Key, tokenInfo.Value,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = tokenInfo.ExpiresAt,
                Path = "/",
                IsEssential = true
            }
        );

        // Refresh Value Cookie
        _httpContextAccessor?.HttpContext?.Response.Cookies.Append(
            Refresh_Token_Key, refreshInfo.Value,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshInfo.ExpiresAt,
                Path = Router.AuthenticationRouter.RefreshToken,
                IsEssential = true
            }
        );
    }

    /// <summary>
    /// Deletes authentication cookies from the client browser
    /// </summary>
    public void ClearTokenCookies()
    {
        var accessTokenCookieOption = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };
        _httpContextAccessor?.HttpContext?.Response.Cookies.Delete(Access_Token_Key, accessTokenCookieOption);

        var refreshTokenCookieOption = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Router.AuthenticationRouter.RefreshToken,
        };

        _httpContextAccessor?.HttpContext?.Response.Cookies.Delete(Refresh_Token_Key, refreshTokenCookieOption);


    }

    /// <summary>
    /// Generates a specialized short-lived token intended for password reset operations.
    /// This includes creating a database-backed <see cref="UserToken"/> record and a JWT representation.
    /// </summary>
    /// <param name="user">The user entity requesting the password reset.</param>
    /// <param name="expiresInMinutes">The duration in minutes for which the reset token remains valid.</param>
    /// <returns>A tuple containing the persistent <see cref="UserToken"/> record and the string-encoded JWT.</returns>
    public (UserToken refreshToken, string AccessToken) GenerateResetToken(User user, int expiresInMinutes)
    {
        var claims = _GetResetClaims(user);

        var jwtAccessTokenObj = _GetJwtSecurityToken(claims, expiresInMinutes);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtAccessTokenObj);


        var validFor = TimeSpan.FromMinutes(expiresInMinutes);

        UserToken refreshToken = new UserToken(user.Id, enTokenType.ResetPasswordToken, null, jwtAccessTokenObj.Id, validFor);

        return (refreshToken, accessToken);

    }



    #region Helper Method(s)

    /// <summary>
    /// Creates a list of claims for a user, including identity information and assigned roles.
    /// These claims will be embedded into the Access Token payload.
    /// </summary>
    /// <param name="User">The user entity containing identity data.</param>
    /// <param name="Roles">A list of roles assigned to the user to be included as role claims.</param>
    /// <returns>A list of <see cref="Claim"/> objects representing the user's identity and permissions.</returns>
    private List<Claim> _GenerateUserClaims(User User, List<string> Roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, User.Id.ToString()),
            new Claim(ClaimTypes.Email, User.Email !),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

        };

        foreach (var role in Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    /// <summary>
    /// Constructs and signs a <see cref="JwtSecurityToken"/> with the provided claims and configuration settings.
    /// </summary>
    /// <param name="claims">The list of claims to be included in the token payload.</param>
    /// <param name="expiresInMinutes">Optional: Override the default expiration time (in minutes).</param>
    /// <returns>A fully configured and signed <see cref="JwtSecurityToken"/> instance.</returns>
    private JwtSecurityToken _GetJwtSecurityToken(List<Claim> claims, int? expiresInMinutes = null)
    {
        return new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes ?? _jwtSettings.AccessTokenExpireDate),
            signingCredentials: new SigningCredentials(_signaturekey, _SecurityAlgorithm)
        );
    }

    /// <summary>
    /// Generates a specific set of claims for password reset tokens.
    /// Includes a security marker to distinguish reset tokens from standard session tokens.
    /// </summary>
    /// <param name="user">The user entity requesting the reset.</param>
    /// <returns>A list of <see cref="Claim"/> containing user identity and the reset-specific marker.</returns>
    private List<Claim> _GetResetClaims(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(SessionTokenClaims.IsResetToken, "true"),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        return claims;

    }

    #endregion

}


