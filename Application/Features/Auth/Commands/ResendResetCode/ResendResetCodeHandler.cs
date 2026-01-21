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
/// Handles resending password reset code with new token
/// </summary>
public class ResendResetCodeHandler : IRequestHandler<ResendResetCodeCommand, Response<bool>>
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;

    private const int OTP_VALIDITY_MINUTES = 3;

    #endregion

    #region Constructor(s)

    public ResendResetCodeHandler(
        IUserService userService,
        IOtpService otpService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IStringLocalizer<SharedResources> localizer,
        ResponseHandler responseHandler)
    {
        _userService = userService;
        _otpService = otpService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler(s)

    public async Task<Response<bool>> Handle(
        ResendResetCodeCommand request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ========================================
            // Step 1: Find user by email
            // ========================================
            var user = await _userService
                .GetUserByEmailAsync(request.dto.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);

                Log.Warning(
                    "Resend reset code attempted for non-existent email: {Email}",
                    request.dto.Email
                );

                return _responseHandler.Success<bool>(true);
            }



            // ========================================
            // Step 2: Check if can resend (cooldown)
            // ========================================

            var canResendResult = await _otpService.CanResendOtpAsync(user.Id, enOtpType.ResetPassword, cancellationToken);

            if (!canResendResult.canResend)
            {
                var secondsRemaining = (int)Math.Ceiling(
                    canResendResult.remaining!.Value.TotalSeconds
                );

                Log.Information(
                    "User {UserId} attempted resend during cooldown. Remaining: {Seconds}s",
                    user.Id,
                    secondsRemaining
                );

                return _responseHandler.BadRequest<bool>(
                    string.Format(
                        _localizer[SharedResourcesKeys.ResendCooldown],
                        secondsRemaining
                    )
                );
            }

            // ========================================
            // Step 3: Regenerate OTP
            // ========================================
            var regenerationResult = await _otpService.RegenerateOtpAsync(
                user.Id,
                enOtpType.ResetPassword,
                OTP_VALIDITY_MINUTES,
                cancellationToken
            );

            if (!regenerationResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                Log.Error(
                    "Failed to regenerate reset OTP for UserId: {UserId}. Errors: {Errors}",
                    user.Id,
                    string.Join(", ", regenerationResult.Errors)
                );

                return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
            }

            Log.Information("Reset OTP regenerated for UserId: {UserId}", user.Id);


            // ========================================
            // Step 4: Save to database
            // ========================================
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Log.Information("Reset code and token saved for UserId: {UserId}", user.Id);

            // ========================================
            // Step 5: Send email (outside transaction)
            // ========================================
            var emailSendResult = await _emailService.SendResetPasswordMessage(
                user.Email!,
                regenerationResult.Data
            );

            return _responseHandler.Success(true);
        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(
                    dex,
                    "Unique constraint violation during resend reset code for email: {Email}",
                    request.dto.Email
                );

                return _responseHandler.BadRequest<bool>(
                    _localizer[SharedResourcesKeys.InvalidExpiredCode]
                );
            }

            Log.Error(
                dex,
                "Database error during resend reset code: {Message}",
                dex.Message
            );

            return _responseHandler.InternalServerError<bool>(
                _localizer[SharedResourcesKeys.UnexpectedError]
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(
                ex,
                "Error in resend reset code for email: {Email}",
                request.dto.Email
            );

            return _responseHandler.InternalServerError<bool>(
                _localizer[SharedResourcesKeys.UnexpectedError]
            );
        }
    }

    #endregion


}
