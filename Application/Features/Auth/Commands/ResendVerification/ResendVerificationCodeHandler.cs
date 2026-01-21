using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class ResendVerificationCodeHandler : IRequestHandler<ResendVerificationCodeCommand, Response<bool>>
{
    #region Field(s)
    private readonly IAuthService _authService;
    private readonly IUserTokenRepository _refreshTokenRepo;
    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;

    // Configuration
    private const int OTP_VALIDITY_MINUTES = 5;

    #endregion

    #region Constructor(s)
    public ResendVerificationCodeHandler(
    IAuthService authService, IUserTokenRepository refreshTokenRepository, IUserService userService,
    IOtpService otpService, IEmailService emailService, IOtpRepository otpRepo,
    IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler
    )
    {
        _authService = authService;
        _refreshTokenRepo = refreshTokenRepository;
        _userService = userService;
        _otpService = otpService;
        _otpRepo = otpRepo;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
        _responseHandler = responseHandler;
        _emailService = emailService;
    }

    #endregion

    #region Handler(s)
    public async Task<Response<bool>> Handle(ResendVerificationCodeCommand request, CancellationToken cancellationToken)
    {


        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _userService.GetUserByEmailAsync(request.DTO.Email).FirstOrDefaultAsync();

            if (user == null)
            {
                await Task.Delay(Random.Shared.Next(100, 300));

                Log.Warning("User not found with email: {Email}", request.DTO.Email);

                return _responseHandler.Success(true);

            }

            if (user.EmailConfirmed)
            {
                Log.Information("Email already confirmed for user Id: {UserId}", user.Id);
                return _responseHandler.Success(true);
            }

            // Check if can resend (cooldown)

            var canResendResult = await _otpService.CanResendOtpAsync(user.Id, enOtpType.ConfirmEmail, cancellationToken);

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



            var regenerationResult = await _otpService.RegenerateOtpAsync(user.Id, enOtpType.ConfirmEmail, OTP_VALIDITY_MINUTES, cancellationToken);

            if (!regenerationResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Error("Failed to regenerate OTP for user Id: {UserId}: {Errors}", user.Id, string.Join(",", regenerationResult.Errors));
                return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
            }


            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);


            string otpCode = regenerationResult.Data;

            var emailSendResult = await _emailService.SendConfirmEmailMessage(user.Email!, otpCode);

            return _responseHandler.Success(true);




        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, $"Unique constraint violation during Resend Verification:{dex.Message}");

                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(dex, $"Database error during email confirmation Resend code:{dex.Message}");

            return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Error in Resend confirm Email otp Process");

            return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }

    }

    #endregion



}

