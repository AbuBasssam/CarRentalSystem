using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;
using System.Net.Mail;

namespace Application.Features.AuthFeature;

public class ResendResetCodeHandler : IRequestHandler<ResendResetCodeCommand, Response<VerificationFlowResponse>>
{
    private readonly IAuthService _authService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _context;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;

    // Configuration
    private const int OTP_VALIDITY_MINUTES = 3;
    private const int TOKEN_VALIDITY_MINUTES = 15;

    public ResendResetCodeHandler(
        IAuthService authService,
        IRefreshTokenRepository refreshTokenRepo,
        IUserService userService,
        IOtpService otpService,
        IEmailService emailService,
        IOtpRepository otpRepo,
        IUnitOfWork unitOfWork,
        IRequestContext context,
        IStringLocalizer<SharedResources> localizer,
        ResponseHandler responseHandler)
    {
        _authService = authService;
        _refreshTokenRepo = refreshTokenRepo;
        _userService = userService;
        _otpService = otpService;
        _emailService = emailService;
        _otpRepo = otpRepo;
        _unitOfWork = unitOfWork;
        _context = context;
        _localizer = localizer;
        _responseHandler = responseHandler;
    }

    public async Task<Response<VerificationFlowResponse>> Handle(
        ResendResetCodeCommand request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            int userId = (int)_context.UserId!;
            var currentJti = _context.TokenJti;

            // Step 2: Get user

            var user = await _userService
                .GetUserByIdAsync(userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Error($"User {userId} not found during resend reset code");

                return _responseHandler.BadRequest<VerificationFlowResponse>(
                    _localizer[SharedResourcesKeys.UserNotFound]
                );
            }

            // Step 3: Get existing OTP with current JTI and expire it

            var existingOtp = _otpRepo
                .GetByTokenJti(currentJti!, enOtpType.ResetPassword)
                .FirstOrDefault();


            if (existingOtp != null)
            {
                existingOtp.ForceExpire();
                Log.Information($"Expired old reset OTP for user {userId}");
            }

            // Step 4: Regenerate OTP

            var regenerationResult = await _otpService.GenerateOtpWithJtiAsync(
                userId,
                enOtpType.ResetPassword,
                OTP_VALIDITY_MINUTES,
                cancellationToken
            );


            if (!regenerationResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Error($"Failed to regenerate reset OTP for user {userId}");

                return _responseHandler.InternalServerError<VerificationFlowResponse>(
                    _localizer[SharedResourcesKeys.UnexpectedError]
                );
            }


            // Step 6: Revoke old reset token

            await _RevokeOldTokenAsync(currentJti!);

            // Step 7: Generate new reset token (stage 1: AwaitingVerification)

            var newToken = _authService.GenerateResetToken(
                user,
                TOKEN_VALIDITY_MINUTES,
                regenerationResult.Data.jti,
                enResetPasswordStage.AwaitingVerification
            );

            await _refreshTokenRepo.AddAsync(newToken.refreshToken);


            // Step 8: Save all changes to database

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Log.Information($"Reset code regenerated for user {userId}");


            // Step 9: Send email (after commit)

            try
            {
                var emailSendResult = await _emailService.SendResetPasswordMessage(
                    user.Email!,
                    regenerationResult.Data.otp
                );

                if (!emailSendResult.IsSuccess)
                {
                    Log.Error($"Failed to send resend reset code email to user {userId}");
                    // Continue - Windows Service will clean up
                }
            }
            catch (SmtpException smtpEx)
            {
                Log.Error(smtpEx, $"SMTP error while sending resend reset code to user {userId}");
                // Continue - Windows Service will clean up
            }
            catch (Exception emailEx)
            {
                Log.Error(emailEx, $"Unexpected error sending reset email to user {userId}");
                // Continue - Windows Service will clean up
            }

            // Step 10: Return new token to frontend

            var response = new VerificationFlowResponse
            {
                Token = newToken.AccessToken,
                ExpiresAt = newToken.refreshToken.ExpiryDate
            };

            return _responseHandler.Success(
                response

            );
        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, $"Unique constraint violation during resend reset code for user {_context.UserId}");

                return _responseHandler.BadRequest<VerificationFlowResponse>(
                    _localizer[SharedResourcesKeys.InvalidExpiredCode]
                );
            }

            Log.Error(dex, $"Database error during resend reset code for user {_context.UserId}");

            return _responseHandler.InternalServerError<VerificationFlowResponse>(
                _localizer[SharedResourcesKeys.UnexpectedError]
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, $"Error in resend reset password code for user {_context.UserId}");

            return _responseHandler.InternalServerError<VerificationFlowResponse>(
                _localizer[SharedResourcesKeys.UnexpectedError]
            );
        }
    }

    /// <summary>
    /// Revoke old reset token by JTI
    /// </summary>
    private async Task _RevokeOldTokenAsync(string jti)
    {
        var isTokenFound = await _refreshTokenRepo.RevokeUserTokenAsync(jti);

        if (isTokenFound)
        {
            Log.Information($"Revoked reset token with JTI {jti}");
        }
        else
        {
            Log.Warning($"No reset token found to revoke with JTI {jti}");
        }
    }
}