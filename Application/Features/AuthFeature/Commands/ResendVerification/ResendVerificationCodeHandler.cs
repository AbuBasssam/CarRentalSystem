using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class ResendVerificationCodeHandler : IRequestHandler<ResendVerificationCodeCommand, Response<string>>
{
    #region Field(s)
    private readonly IAuthService _authService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _context;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public ResendVerificationCodeHandler(
    IAuthService authService,
    IRefreshTokenRepository refreshTokenRepository,
    IUserService userService,
    IOtpService otpService,
    IOtpRepository otpRepo,
    IUnitOfWork unitOfWork,
    IRequestContext context,
    IStringLocalizer<SharedResources> localizer,
    ResponseHandler responseHandler
    )
    {
        _authService = authService;
        _refreshTokenRepo = refreshTokenRepository;
        _userService = userService;
        _otpService = otpService;
        _otpRepo = otpRepo;
        _unitOfWork = unitOfWork;
        _context = context;
        _localizer = localizer;
        _responseHandler = responseHandler;
    }
    #endregion

    #region Handler(s)
    public async Task<Response<string>> Handle(ResendVerificationCodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Step 1: Validate verification token
            var token = _context.AuthToken;
            if (string.IsNullOrEmpty(token))
            {
                Log.Warning("Resend verification attempt without token");
                return _responseHandler.Unauthorized<string>();
            }

            var isTokenValid = await _authService.ValidateSessionToken(
                token,
                enTokenType.VerificationToken);

            if (!isTokenValid)
            {
                Log.Warning("Invalid verification token");

                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.Unauthorized<string>(
                    _localizer[SharedResourcesKeys.InvalidToken]);
            }

            // Step 2: Extract email from token
            var getEmailResult = _authService.GetEmailFromSessionToken(token);
            if (!getEmailResult.IsSuccess)
            {
                Log.Error("Failed to extract email from token");
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.BadRequest<string>(getEmailResult.Errors.First());
            }

            string email = getEmailResult.Data!;
            Log.Information("Processing resend verification for email: {Email}", email);

            // Step 3: Validate user exists
            var user = await _userService.GetUserByEmailAsync(email).FirstOrDefaultAsync();
            if (user == null)
            {
                Log.Warning("User not found for email: {Email}", email);
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.NotFound<string>(
                    _localizer[SharedResourcesKeys.UserNotFound]);
            }

            // Step 4: Check if already verified
            if (user.EmailConfirmed)
            {
                Log.Information("Email already verified for user: {UserId}", user.Id);
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.BadRequest<string>(
                    _localizer[SharedResourcesKeys.EmailAlreadyVerified]);
            }



            // Step 5: Check cooldown period (2 minutes between requests)
            var lastOtp = await _otpRepo.GetLatestValidOtpAsync(
                user.Id,
                enOtpType.ConfirmEmail
                ).FirstOrDefaultAsync();

            if (lastOtp != null)
            {
                var remainingCooldown = lastOtp.GetRemainingCooldown();

                if (remainingCooldown.HasValue)
                {
                    var remainingSeconds = (int)remainingCooldown.Value.TotalSeconds;

                    Log.Information(
                        "Cooldown active for user {UserId}. Remaining: {Seconds}s",
                        user.Id,
                        remainingSeconds);

                    await transaction.RollbackAsync(cancellationToken);
                    return _responseHandler.BadRequest<string>(
                        string.Format(
                            _localizer[SharedResourcesKeys.ResendCooldown],
                            remainingSeconds));
                }

                // Expire old OTP
                lastOtp.ForceExpire();
                lastOtp.MarkAsUsed();
                _otpRepo.Update(lastOtp);

                Log.Information("Expired old OTP for user: {UserId}", user.Id);
            }

            // Step 7: Generate and send new OTP
            await _otpService.GenerateAndSendOtpAsync(
               user.Id,
               user.Email!,
                enOtpType.ConfirmEmail,
                 5
              );

            Log.Information("New verification code sent to user: {UserId}", user.Id);

            // Step 8: Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return _responseHandler.Success(
                string.Empty
               );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Error resending verification code");

            return _responseHandler.InternalServerError<string>(
                _localizer[SharedResourcesKeys.ErrorOccurred]);
        }
    }

    #endregion
}

