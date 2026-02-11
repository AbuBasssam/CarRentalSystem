using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Domain.HelperClasses;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

/// <summary>
/// Handles token refresh requests with comprehensive security validations
/// </summary>
public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Response<JwtAuthResult>>
{
    #region Fields
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IRequestContext _requestContext;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;
    #endregion

    #region Constructor
    public RefreshTokenHandler(IAuthService authService, IUserService userService, ITokenService tokenService,
        IUnitOfWork unitOfWork, ResponseHandler responseHandler,
        IStringLocalizer<SharedResources> localizer, IRequestContext requestContext)
    {
        _authService = authService;
        _userService = userService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
        _localizer = localizer;
        _requestContext = requestContext;
    }
    #endregion

    #region Handle
    public async Task<Response<JwtAuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var refreshToken = _requestContext.RefreshToken;

        // ===== Step 1: Validate Cookie Presence =====
        if (string.IsNullOrEmpty(refreshToken))
        {
            Log.Warning(
                "Refresh token missing in cookies for user with Email {email}",
                request.Email
            );
            _tokenService.ClearTokenCookies();
            return _responseHandler.Unauthorized<JwtAuthResult>(meta: new
            {
                errorCode = enErrorCode.MissingToken.ToString(),
                isRecoverable = false
            }
            );
        }

        // ===== Step 2: Validate User exists =====

        var user = await _userService.GetUserByEmailAsync(request.Email).FirstOrDefaultAsync();

        if (user == null)
        {
            await Task.Delay(Random.Shared.Next(100, 300));

            _tokenService.ClearTokenCookies();

            Log.Warning($"Refresh tokenattempted for non-existent email: {request.Email}");
            return _responseHandler.Unauthorized<JwtAuthResult>(meta: new
            {
                errorCode = enErrorCode.InvalidToken.ToString(),
                isRecoverable = false
            });
        }



        // ===== Step 3: Database Token Validation =====
        var (refreshTokenEntity, validateException) = await _authService
            .ValidateRefreshToken(user.Id, refreshToken);

        if (refreshTokenEntity == null)
        {
            Log.Warning(
                "Invalid refresh token for user {UserId}: {Error}",
                user.Id,
                validateException?.Message ?? "Unknown error"
            );
            _tokenService.ClearTokenCookies();
            return _responseHandler.Unauthorized<JwtAuthResult>(
                _localizer[SharedResourcesKeys.InvalidToken],
                meta: new
                {
                    errorCode = enErrorCode.SessionExpired.ToString(),
                    isRecoverable = false
                }
            );
        }

        // ===== Step 4: 🚨 TOKEN REUSE DETECTION =====
        if (refreshTokenEntity.IsUsed)
        {
            Log.Error(
                "🚨 SECURITY ALERT: Token reuse detected! " +
                "User: {UserId}, " +
                "Originally used at: {UsedAt}, " +
                "Reuse attempt at: {Now}",
                user.Id,
                refreshTokenEntity.RevokedAt ?? refreshTokenEntity.ExpiryDate,
                DateTime.UtcNow
            );

            // Revoke ALL active sessions for this user
            await _authService.LogoutFromAllDevices(user.Id);

            _tokenService.ClearTokenCookies();

            return _responseHandler.Unauthorized<JwtAuthResult>(meta: new
            {
                errorCode = enErrorCode.AccessDenied.ToString(),
                isRecoverable = false
            });
        }




        // ===== Step 4: Email Verification Check =====
        if (!user.EmailConfirmed)
        {
            Log.Warning(
                "Token refresh attempt for unverified email - User: {UserId}, Email: {Email}",
                user.Id,
                user.Email
            );
            _tokenService.ClearTokenCookies();
            return _responseHandler.BadRequest<JwtAuthResult>(
                _localizer[SharedResourcesKeys.EmailNotVerified]
            );
        }


        // ===== Step 5: Token Rotation Mark old token as used and revoked =====

        refreshTokenEntity.Revoke();
        refreshTokenEntity.ForceExpire();

        // ===== Step 6: Generate New Token Pair =====
        var newJwtAuth = await _authService.GetJwtAuthForuser(user);

        // ===== Step 7: Persist Changes =====
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Log.Information(
            "Token refresh successful - " +
            "User: {UserId}, Email: {Email}",
            user.Id,
            user.Email
        );

        return _responseHandler.Success(newJwtAuth);
    }
    #endregion
}