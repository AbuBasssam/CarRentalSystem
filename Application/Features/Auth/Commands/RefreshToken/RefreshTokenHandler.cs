using Application.Models;
using ApplicationLayer.Resources;
using Domain.HelperClasses;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Response<JwtAuthResult>>
{
    #region Fields
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IRequestContext _requestContext;
    private readonly IUserTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;
    #endregion

    #region Constructor
    public RefreshTokenHandler(
        IAuthService authService,
        IUserService userService,
        IUserTokenRepository refreshTokenRepo,
        IUnitOfWork unitOfWork,
        ResponseHandler responseHandler,
        IStringLocalizer<SharedResources> localizer,
        IRequestContext requestContext)
    {
        _authService = authService;
        _userService = userService;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
        _localizer = localizer;
        this._requestContext = requestContext;
    }
    #endregion

    #region Handle
    public async Task<Response<JwtAuthResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId!;
        // Validate Refresh Token in database
        var refreshToken = _requestContext.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
        {
            Log.Warning("No refresh token provided in request for user {UserId}", userId);
            return _responseHandler.Unauthorized<JwtAuthResult>();
        }
        var (refreshTokenEntity, validateException) = await _authService
            .ValidateRefreshToken(userId.Value, refreshToken, _requestContext.TokenJti!);

        if (refreshTokenEntity == null)
        {
            Log.Warning(
                "Invalid refresh token for user {UserId}: {Error}",
                userId,
                validateException?.Message
            );

            return _responseHandler.Unauthorized<JwtAuthResult>();
        }
        refreshTokenEntity.Revoke();
        refreshTokenEntity.ForceExpire();





        var user = await _userService.GetUserByIdAsync(userId.Value).FirstOrDefaultAsync();

        // Step 11: Generate new token pair
        var newJwtAuth = await _authService.GetJwtAuthForuser(user!);

        // Step 12: Save changes
        await _unitOfWork.SaveChangesAsync();

        Log.Information(
            "Token refresh successful for user {UserId}. " +
            "Old token {OldJti} revoked, new token generated.",
            userId,
            _requestContext.TokenJti
        );

        return _responseHandler.Success(newJwtAuth);
    }
    #endregion


}