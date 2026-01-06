using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;
public class VerifyResetCodeCommandHandler : IRequestHandler<VerifyResetCodeCommand, Response<VerificationFlowResponse>>
{

    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly IRequestContext _context;

    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;
    private const int TOKEN_VALIDITY_MINUTES = 5;


    public VerifyResetCodeCommandHandler(IUserService userService, IAuthService authService, IOtpService otpService, IRequestContext context,
      IRefreshTokenRepository refreshTokenRepo, IOtpRepository otpRepo, IUnitOfWork unitOfWork,
      IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler)
    {

        _userService = userService;
        _authService = authService;
        _otpService = otpService;
        _context = context;
        _refreshTokenRepo = refreshTokenRepo;

        _otpRepo = otpRepo;
        _unitOfWork = unitOfWork;

        _localizer = localizer;
        _responseHandler = responseHandler;
    }

    public async Task<Response<VerificationFlowResponse>> Handle(VerifyResetCodeCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var userId = _context.UserId;

            // Step 2: Get latest OTP with matching JTI
            var otp = _otpRepo.GetLatestValidOtpAsync(userId, enOtpType.ResetPassword)
                .FirstOrDefault(o => o.TokenJti == _context.TokenJti);

            // ========= 3. Validate OTP =========
            if (otp == null || !otp.IsValidForVerification())
            {
                Log.Warning(
                $"OTP validation failed: no active OTP found for UserId {userId}");

                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);

            }

            // Verify OTP code
            var hashedCode = Helpers.HashString(request.DTO.Code);
            if (otp.Code != hashedCode)
            {
                otp.IncrementAttempts();

                if (otp.HasExceededMaxAttempts())
                {
                    otp.ForceExpire();
                    otp.MarkAsUsed();

                    Log.Warning($"OTP locked after max attempts for UserId {userId}");

                    await _RevokeCurrentTokenAsync(_context.TokenJti);


                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
                }
                else
                    Log.Warning($"OTP validation failed: invalid code for UserId {userId}");

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            // Step 3: Code is valid - Get user
            var user = await _userService
                .GetUserByIdAsync(userId)
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);

                Log.Error($"User {userId} not found during verification");

                return _responseHandler.BadRequest<VerificationFlowResponse>(
                    _localizer[SharedResourcesKeys.UserNotFound]
                );
            }

            otp.MarkAsUsed();

            await _refreshTokenRepo.RevokeUserTokenAsync(_context.TokenJti);

            // Generate new JTI for stage 2 token
            var newJti = Guid.NewGuid().ToString();

            // Update OTP with new JTI for tracking
            otp.UpdateTokenJti(newJti);

            // Generate stage 2 token (Verified)
            var newToken = _authService.GenerateResetToken(user, TOKEN_VALIDITY_MINUTES, newJti, enResetPasswordStage.Verified);

            await _refreshTokenRepo.AddAsync(newToken.refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            Log.Information($"Code verified successfully for user {userId}");
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
                await transaction.RollbackAsync(cancellationToken);

                Log.Warning(dex, "duplicate transaction");

                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            await transaction.RollbackAsync(cancellationToken);

            Log.Error(dex, "Database update error during Reset Password Verifying");

            return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, $"Error verifying reset code for userId: {_context.UserId}");

            return _responseHandler.InternalServerError<VerificationFlowResponse>("An error occurred while verifying the code.");
        }




    }
    /// <summary>
    /// Revoke current reset token by JTI
    /// </summary>
    private async Task _RevokeCurrentTokenAsync(string jti)
    {

        var isTokenfound = await _refreshTokenRepo.RevokeUserTokenAsync(jti);

        if (isTokenfound)
        {
            Log.Information("Revoked reset token with JTI {Jti}", jti);
        }
        else
        {
            Log.Warning("No token found to revoke with JTI {Jti}", jti);
        }


    }
}
