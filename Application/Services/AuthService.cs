using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.Enums;
using Domain.HelperClasses;
using Domain.Security;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Application.Services;

public class AuthService : IAuthService
{
    #region Field(s)
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<User> _userManager;

    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _Localizer;




    private readonly SymmetricSecurityKey _signaturekey;
    private static string _SecurityAlgorithm = SecurityAlgorithms.HmacSha256Signature;
    private static JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
    #endregion

    #region Constructor(s)
    public AuthService(JwtSettings jwtSettings, IRefreshTokenRepository refreshTokenRepo,
        IUserService userService, IOtpRepository otpRepo, UserManager<User> userManager,
        IUnitOfWork unitOfWork, IOtpService otpService, IStringLocalizer<SharedResources> localizer, IHttpContextAccessor httpContextAccessor)
    {
        _jwtSettings = jwtSettings;
        _refreshTokenRepo = refreshTokenRepo;
        _signaturekey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret!));
        _userService = userService;
        _otpRepo = otpRepo;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _Localizer = localizer;
        _httpContextAccessor = httpContextAccessor;
    }
    #endregion

    #region Method(s)
    public async Task<JwtAuthResult> GetJwtAuthForuser(User User)
    {

        // 1) Revoke Existing Token
        await _RevokeActiveUserToken(User.Id, enTokenType.AuthToken);

        // 2) Generate jwtAccessTokon Object And String
        var (jwtAccessTokenObj, jwtAccessTokenString) = await _GenerateAccessToken(User);

        // 3) Generate TokenValidation Object
        var refreshTokenObj = _GenerateRefreshToken(User);

        // 4) Generate the JwtAuth for the user
        JwtAuthResult jwtAuthResult = _GetJwtAuthResult(jwtAccessTokenString, $"{User.FirstName} {User.LastName}");

        // 5) Save AccessToken, TokenValidation In UserToken Table
        UserToken refreshTokenEntity = _GetUserRefreshToken(User, jwtAccessTokenObj, refreshTokenObj);

        var result = await _refreshTokenRepo.AddAsync(refreshTokenEntity);

        SetRefreshTokenCookie(refreshTokenObj.Value, refreshTokenObj.ExpiresAt);

        await _unitOfWork.SaveChangesAsync();

        return jwtAuthResult;
    }

    public Result<string> GetEmailFromSessionToken(string sessionToken)
    {
        (JwtSecurityToken? jwtAccessTokenObj, Exception? exception) = GetJwtAccessTokenObjFromAccessTokenString(sessionToken);

        if (jwtAccessTokenObj == null)
            return Result<string>.Failure([_Localizer[SharedResourcesKeys.InvalidToken]]);

        (string email, Exception? emailException) = _GetUserEmailFromJwtAccessTokenObj(jwtAccessTokenObj);
        if (string.IsNullOrEmpty(email)) return Result<string>.Failure([_Localizer[SharedResourcesKeys.FailedExtractEmail]]);

        return Result<string>.Success(email);
    }

    public Result<int> GetUserIdFromSessionToken(string sessionToken)
    {
        (JwtSecurityToken? jwtAccessTokenObj, Exception? exception) =
            GetJwtAccessTokenObjFromAccessTokenString(sessionToken);

        if (jwtAccessTokenObj == null)
            return Result<int>.Failure([
                _Localizer[SharedResourcesKeys.InvalidToken]
            ]);

        (int userId, Exception? userIdException) =
            GetUserIdFromJwtAccessTokenObj(jwtAccessTokenObj);

        if (userId == 0)
            return Result<int>.Failure([
                //_Localizer[SharedResourcesKeys.FailedExtractUserId]
               string.Empty
            ]);

        return Result<int>.Success(userId);
    }

    public (JwtSecurityToken?, Exception?) GetJwtAccessTokenObjFromAccessTokenString(string AccessToken)
    {
        try
        {
            return ((JwtSecurityToken)_tokenHandler.ReadToken(AccessToken), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    public (ClaimsPrincipal?, Exception?) GetClaimsPrinciple(string AccessToken)
    {
        var parameters = _GetTokenValidationParameters();

        try
        {
            var principal = _tokenHandler.ValidateToken(AccessToken, parameters, out SecurityToken validationToken);

            if (validationToken is JwtSecurityToken jwtToken && jwtToken.Header.Alg.Equals(_SecurityAlgorithm))
                return (principal, null);

            return (null, new ArgumentNullException(_Localizer[SharedResourcesKeys.ClaimsPrincipleIsNull]));
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    public (int, Exception?) GetUserIdFromJwtAccessTokenObj(JwtSecurityToken jwtAccessTokenObj)
    {
        if (!int.TryParse(jwtAccessTokenObj.Claims.FirstOrDefault(x => x.Type == nameof(UserClaimModel.Id))?.Value, out int UserId))
            return (0, new ArgumentNullException(_Localizer[SharedResourcesKeys.InvalidUserId]));

        return (UserId, null);
    }

    public string? GetJtiFromAccessTokenString(string accessToken)
    {
        var (principal, error) = GetClaimsPrinciple(accessToken);

        if (principal == null || error != null)
            return null;

        var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti);

        return jtiClaim?.Value;
    }

    public bool IsValidAccessToken(string AccessTokenStr)
    {
        try
        {
            var (jwtAccessTokenObj, jwtAccesTokenEx) = GetJwtAccessTokenObjFromAccessTokenString(AccessTokenStr);
            if (jwtAccesTokenEx != null) return false;

            GetClaimsPrinciple(AccessTokenStr);

            if (jwtAccessTokenObj!.ValidTo < DateTime.UtcNow) return false;


            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);

            return false;
        }
    }

    public async Task<(UserToken?, Exception?)> ValidateRefreshToken(int UserId, string refreshToken, string jwtId)
    {

        var refreshTokenEntity = await _refreshTokenRepo
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == UserId &&
                x.JwtId == jwtId &&
                x.Type.Equals(enTokenType.AuthToken)
            );

        if (refreshTokenEntity == null)
            return (null, new SecurityTokenArgumentException(_Localizer[SharedResourcesKeys.NullRefreshToken]));


        if (!BCrypt.Net.BCrypt.Verify(refreshToken, refreshTokenEntity.RefreshToken))
            return (null, new SecurityTokenArgumentException(_Localizer[SharedResourcesKeys.InvalidToken]));

        if (refreshTokenEntity.ExpiryDate < DateTime.UtcNow)
        {
            refreshTokenEntity.Revoke();
            return (null, new SecurityTokenArgumentException(_Localizer[SharedResourcesKeys.RevokedRefreshToken]));
        }
        // Check if already revoked
        if (refreshTokenEntity.IsRevoked)
        {
            return (null, new SecurityTokenArgumentException(
                _Localizer[SharedResourcesKeys.RevokedRefreshToken]
            ));
        }

        return (refreshTokenEntity, null);
    }


    /// <summary>
    /// Logout user by revoking their access and refresh tokens
    /// </summary>
    public async Task<Result<bool>> Logout(int userId, string jwtId)
    {
        try
        {
            // Step 1: Find the token by JWT ID
            var tokenEntity = await _refreshTokenRepo.GetTableAsTracking()
                .Where(x =>
                    x.JwtId == jwtId &&
                    x.UserId == userId &&
                    x.Type == enTokenType.AuthToken
                )
                .FirstOrDefaultAsync();

            if (tokenEntity == null)
            {
                return Result<bool>.Failure([
                    _Localizer[SharedResourcesKeys.TokenNotFound]
                ]);
            }

            // Step 2: Check if already revoked
            if (tokenEntity.IsRevoked)
            {
                Log.Information("User {UserId} attempted to logout with already revoked token {JwtId}", userId, jwtId);

                return Result<bool>.Success(true); // Already logged out
            }

            // Step 3: Revoke the token
            _RevokeToken(tokenEntity);


            Log.Information("User {UserId} logged out successfully. Token {JwtId} revoked.", userId, jwtId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during logout for user {UserId}", userId);
            return Result<bool>.Failure([
                _Localizer[SharedResourcesKeys.LogoutFailed]
            ]);
        }
    }

    /// <summary>
    /// Logout from all devices by revoking all active tokens
    /// </summary>
    public async Task<Result<bool>> LogoutFromAllDevices(int userId)
    {
        try
        {
            // Step 1: Get all active tokens for user
            var activeTokens = await _refreshTokenRepo
                .GetTableNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    !x.IsRevoked &&
                    x.Type == enTokenType.AuthToken
                )
                .ToListAsync();

            if (!activeTokens.Any())
            {
                return Result<bool>.Success(true); // No active tokens
            }

            // Step 2: Revoke all tokens
            foreach (var token in activeTokens)
            {
                _RevokeToken(token);
            }


            Log.Information(
                "User {UserId} logged out from all devices. {Count} tokens revoked.",
                userId,
                activeTokens.Count
            );

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during logout from all devices for user {UserId}", userId);
            return Result<bool>.Failure([
                _Localizer[SharedResourcesKeys.LogoutFailed]
            ]);
        }
    }

    public (UserToken refreshToken, string AccessToken) GenerateResetToken(User user, int expiresInMinutes)
    {
        var claims = _GetResetClaims(user);

        var (jwtAccessTokenObj, AccessToken) = _GenerateSessionToken(claims, expiresInMinutes);

        var validFor = TimeSpan.FromMinutes(expiresInMinutes);

        UserToken refreshToken = new UserToken(user.Id, enTokenType.ResetPasswordToken, null, jwtAccessTokenObj.Id, validFor);

        return (refreshToken, AccessToken);

    }

    public void SetRefreshTokenCookie(string refreshToken, DateTime refreshTokenExpires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshTokenExpires
        };
        _httpContextAccessor?.HttpContext?.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    public void DeleteRefreshTokenCookie()
    {
        _httpContextAccessor?.HttpContext?.Response.Cookies.Delete("refreshToken");
    }

    #endregion

    #region AccessToken Methods
    private List<Claim> _GenerateUserClaims(User User, List<string> Roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, User.UserName!),
            new Claim(ClaimTypes.Name, User.UserName !),
            new Claim(ClaimTypes.Email, User.Email !),
            new Claim(ClaimTypes.MobilePhone, User.PhoneNumber ??""),
            new Claim(nameof(UserClaimModel.Id), User.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

        };

        foreach (var role in Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    private async Task<(JwtSecurityToken, string)> _GenerateAccessToken(User User)
    {
        List<string> roles = await _userService.GetUserRolesAsync(User);

        var claims = _GenerateUserClaims(User, roles);

        JwtSecurityToken Obj = _GetJwtSecurityToken(claims);

        var Value = new JwtSecurityTokenHandler().WriteToken(Obj);

        return (Obj, Value);
    }

    private JwtSecurityToken _GetJwtSecurityToken(List<Claim> claims)
    {
        return new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireDate),
            signingCredentials: new SigningCredentials(_signaturekey, _SecurityAlgorithm)
        );
    }

    private TokenValidationParameters _GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = _jwtSettings.ValidateIssuer,
            ValidIssuers = new[] { _jwtSettings.Issuer },

            ValidateAudience = _jwtSettings.ValidateAudience,
            ValidAudience = _jwtSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret!)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private (string, Exception?) _GetUserEmailFromJwtAccessTokenObj(JwtSecurityToken jwtAccessTokenObj)
    {
        var emailClaim = jwtAccessTokenObj.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        return string.IsNullOrEmpty(emailClaim) ?
            ("", new ArgumentNullException(_Localizer[SharedResourcesKeys.InvalidEmailClaim])) : (emailClaim, null);
    }

    #endregion

    #region RefreshToken Methods
    private RefreshToken _GenerateRefreshToken(User User)
    {
        return new RefreshToken()
        {
            Username = User.UserName!,
            Value = Helpers.GenerateRandomString64Length(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpireDate)
        };
    }

    #endregion

    #region Helpers

    private (JwtSecurityToken, string) _GenerateSessionToken(List<Claim> claims, int expiresInMinutes)
    {
        var creds = new SigningCredentials(_signaturekey, _SecurityAlgorithm);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (token, accessToken);
    }

    private List<Claim> _GetResetClaims(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(SessionTokenClaims.IsResetToken, "true"),
            new Claim(nameof(UserClaimModel.Id), user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        return claims;

    }

    private async Task _RevokeActiveUserToken(int userId, enTokenType tokenType)
    {
        var existingTokens = await _refreshTokenRepo
           .GetTableAsTracking().
           FirstOrDefaultAsync(x => x.UserId == userId && !x.IsRevoked && x.Type == tokenType);

        if (existingTokens != null)
        {
            existingTokens.Revoke();
            existingTokens.ForceExpire();
        }
    }

    private static JwtAuthResult _GetJwtAuthResult(string jwtAccessTokenString, string fullName)
    {


        return new JwtAuthResult
        {
            AccessToken = jwtAccessTokenString,
            FullName = fullName
        };
    }

    private UserToken _GetUserRefreshToken(User User, JwtSecurityToken jwtAccessTokenObj, RefreshToken refreshTokenObj)
    {
        var refreshToken = Helpers.HashString(refreshTokenObj.Value);

        return UserToken.GenerateAuthToken(User.Id, refreshToken, jwtAccessTokenObj.Id, refreshTokenObj.ExpiresAt);


    }

    /// <summary>
    /// Revokes a token entity
    /// </summary>
    private void _RevokeToken(UserToken tokenEntity)
    {
        tokenEntity.Revoke();

        tokenEntity.ForceExpire();

    }

    #endregion

}
