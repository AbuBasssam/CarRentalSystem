using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.Enums;
using Domain.HelperClasses;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Data;
using System.IdentityModel.Tokens.Jwt;


namespace Application.Services;

public class AuthService : IAuthService
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IUserTokenRepository _refreshTokenRepo;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _Localizer;
    #endregion

    #region Constructor(s)
    public AuthService(IUserService userService, ITokenService tokenService, IOtpService otpService,
        IUserTokenRepository refreshTokenRepo, IOtpRepository otpRepo,
        IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer)
    {
        _userService = userService;
        _otpService = otpService;
        _tokenService = tokenService;

        _refreshTokenRepo = refreshTokenRepo;
        _otpRepo = otpRepo;
        _unitOfWork = unitOfWork;

        _Localizer = localizer;
    }
    #endregion

    #region Method(s)
    public async Task<JwtAuthResult> GetJwtAuthForuser(User User)
    {

        // 1) Revoke Existing Token
        await _RevokeActiveUserToken(User.Id, enTokenType.AuthToken);

        // 2) Generate jwtAccessTokon Object And String
        var (jwtAccessTokenObj, jwtAccessTokenString) = await _tokenService.GenerateAccessToken(User);

        // 3) Generate TokenValidation Object
        var refreshTokenObj = _tokenService.GenerateRefreshToken();

        // 4) Generate the JwtAuth for the user
        JwtAuthResult jwtAuthResult = new JwtAuthResult
        {
            FirstName = User.FirstName,
            LastName = User.LastName,

        };

        // 5) Save AccessToken, TokenValidation In UserToken Table
        UserToken refreshTokenEntity = _GetUserRefreshToken(User, jwtAccessTokenObj, refreshTokenObj);

        var result = await _refreshTokenRepo.AddAsync(refreshTokenEntity);

        TokenInfo RefreshTokenInfo = new TokenInfo
        {
            Value = refreshTokenObj.Value,
            ExpiresAt = refreshTokenObj.ExpiresAt
        };
        TokenInfo accessTokenInfo = new TokenInfo
        {
            Value = jwtAccessTokenString,
            ExpiresAt = jwtAccessTokenObj.ValidTo
        };


        _tokenService.SetTokenCookies(accessTokenInfo, RefreshTokenInfo);

        await _unitOfWork.SaveChangesAsync();

        return jwtAuthResult;
    }

    public async Task<(UserToken?, Exception?)> ValidateRefreshToken(
     int userId, string refreshToken)
    {
        var refreshTokenEntity = await _refreshTokenRepo
            .GetActiveSessionTokenByUserId(userId, enTokenType.AuthToken)
            .FirstOrDefaultAsync();



        if (refreshTokenEntity == null)
            return (null, new SecurityTokenArgumentException(
                _Localizer[SharedResourcesKeys.NullRefreshToken]));


        if (!BCrypt.Net.BCrypt.Verify(refreshToken, refreshTokenEntity.RefreshToken))
            return (null, new SecurityTokenArgumentException(
                _Localizer[SharedResourcesKeys.InvalidToken]));

        //double-check
        if (refreshTokenEntity.IsExpired())
        {
            refreshTokenEntity.Revoke();
            return (null, new SecurityTokenArgumentException(
                _Localizer[SharedResourcesKeys.RevokedRefreshToken]));
        }

        //double-check
        if (refreshTokenEntity.IsRevoked)
            return (null, new SecurityTokenArgumentException(
                _Localizer[SharedResourcesKeys.RevokedRefreshToken]));

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

            _tokenService.ClearTokenCookies();

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

    #endregion


    #region Helpers
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



    private UserToken _GetUserRefreshToken(User User, JwtSecurityToken jwtAccessTokenObj, TokenInfo refreshTokenInfo)
    {
        var refreshToken = Helpers.HashString(refreshTokenInfo.Value);

        return UserToken.GenerateAuthToken(User.Id, refreshToken, jwtAccessTokenObj.Id, refreshTokenInfo.ExpiresAt);


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
