using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

/// <summary>
/// Handles verification of password reset code
/// </summary>
public class VerifyResetCodeHandler : IRequestHandler<VerifyResetCodeCommand, Response<VerificationFlowResponse>>
{

    #region Field(s)

    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;

    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;
    private const int TOKEN_VALIDITY_MINUTES = 15;

    #endregion

    #region Constructor(s)

    /// <summary>
    /// Initializes a new instance of VerifyResetCodeHandler
    /// </summary>
    public VerifyResetCodeHandler(IUserService userService, IAuthService authService, IOtpService otpService,
  IRefreshTokenRepository refreshTokenRepo, IOtpRepository otpRepo, IUnitOfWork unitOfWork,
  IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler)
    {

        _userService = userService;
        _authService = authService;
        _otpService = otpService;
        _refreshTokenRepo = refreshTokenRepo;

        _otpRepo = otpRepo;
        _unitOfWork = unitOfWork;

        _localizer = localizer;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler(s)

    /// <summary>
    /// Processes OTP verification for password reset
    /// </summary>
    /// <param name="request">Verify reset code command containing OTP</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing verified token for password reset</returns>
    public async Task<Response<VerificationFlowResponse>> Handle(VerifyResetCodeCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // =========  Load User =========
            var user = await _userService
                .GetUserByEmailAsync(request.DTO.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await Task.Delay(Random.Shared.Next(100, 300));

                Log.Warning($"Email confirmation attempted for non-existent email: {request.DTO.Email}");

                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            // ========= Validate OTP =========

            var otpValidationResult = await _otpService.ValidateOtp(user.Id, request.DTO.OtpCode, enOtpType.ResetPassword, cancellationToken);

            if (!otpValidationResult.IsValid)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }


            otpValidationResult.Otp?.ForceExpire();


            // Generate reset token
            var newToken = _authService.GenerateResetToken(user, TOKEN_VALIDITY_MINUTES);

            await _refreshTokenRepo.AddAsync(newToken.refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            Log.Information($"Code verified successfully for user {user.Id}");
            var response = new VerificationFlowResponse
            {
                Token = newToken.AccessToken,
                ExpiresAt = newToken.refreshToken.ExpiryDate
            };
            return _responseHandler.Success(response);

        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {

                Log.Warning(dex, $"duplicate transaction:{dex.Message}");

                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }


            Log.Error(dex, $"Database update error during Reset Password Verifying: {dex.Message}");

            return _responseHandler.InternalServerError<VerificationFlowResponse>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, $"Error verifying reset code for user with email: {request.DTO.Email}: {ex.Message}");

            return _responseHandler.InternalServerError<VerificationFlowResponse>("An error occurred while verifying the code.");
        }




    }

    #endregion


}
