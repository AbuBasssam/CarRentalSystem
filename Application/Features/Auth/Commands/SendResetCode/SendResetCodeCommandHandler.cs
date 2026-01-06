using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class SendResetCodeCommandHandler : IRequestHandler<SendResetCodeCommand, Response<VerificationFlowResponse>>
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IAuthService _authServic;

    private readonly IOtpRepository _otpRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IStringLocalizer<SharedResources> _localizer;

    private readonly ResponseHandler _responseHandler;
    // Configuration constants
    private const int OTP_VALIDITY_MINUTES = 3;
    private const int TOKEN_VALIDITY_MINUTES = 15;

    public SendResetCodeCommandHandler(IEmailService emailService, IUserService userService, IAuthService authServic,
        IOtpRepository otpRepo, IRefreshTokenRepository refreshTokenRepo, IUnitOfWork unitOfWork,
        IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler)
    {
        _emailService = emailService;
        _userService = userService;
        _authServic = authServic;

        _otpRepo = otpRepo;
        _refreshTokenRepo = refreshTokenRepo;

        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
        _localizer = localizer;
    }

    public async Task<Response<VerificationFlowResponse>> Handle(SendResetCodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        var email = request.DTO.Email;

        try
        {
            // Step 1: Find user by email 
            var user = await _userService.GetUserByEmailAsync(email).FirstOrDefaultAsync();

            if (user == null)
            {

                // Security: Always return success to prevent email enumeration
                Log.Warning($"Password reset requested for non-existent email: {email}");

                return _GetSuccessResponse(string.Empty, DateTime.UtcNow.AddMinutes(TOKEN_VALIDITY_MINUTES));

            }

            // Step 2: Check for existing active reset codes
            var existingOtp = _otpRepo
                .GetLatestValidOtpAsync(user.Id, enOtpType.ResetPassword)
                .FirstOrDefault(o => !o.IsUsed && !(o.ExpirationTime <= DateTime.UtcNow));

            if (existingOtp != null)
            {
                // Force expire existing OTP
                existingOtp.ForceExpire();
                Log.Information($"Expired existing reset code for user {user.Id}");
            }


            // Step 3: Generate new OTP code
            var otpCode = Helpers.GenerateOtp();

            var hashedCode = Helpers.HashString(otpCode);

            var jti = Guid.NewGuid().ToString();

            var validFor = TimeSpan.FromMinutes(OTP_VALIDITY_MINUTES);

            var otp = new Otp(hashedCode, enOtpType.ResetPassword, user.Id, validFor, jti);


            // Step 4: Generate JWT token for stage 1 (AwaitingVerification)
            var token = _authServic.GenerateResetToken(user, TOKEN_VALIDITY_MINUTES, jti, enResetPasswordStage.AwaitingVerification);


            // Step 5: Save to DB 

            await _otpRepo.AddAsync(otp);

            await _refreshTokenRepo.AddAsync(token.refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            Log.Information($"OTP and token saved for user {user.Id}. Attempting to send email.");


            // Step 5: Send email (don't save OTP if email fails)
            var emailResult = await _emailService.SendResetPasswordMessage(user.Email!, otpCode);

            if (!emailResult.IsSuccess)
            {
                return _GetSuccessResponse(string.Empty, DateTime.UtcNow.AddMinutes(TOKEN_VALIDITY_MINUTES));


            }

            Log.Information("Password reset code sent successfully to user {UserId}", user.Id);

            return _GetSuccessResponse(token.AccessToken, (DateTime)token.refreshToken.ExpiryDate!);
        }
        catch (DbUpdateException dex) when (dex.IsUniqueConstraintViolation())
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Warning($"Active OTP constraint violation for email {request.DTO.Email}. Possible race condition.");

            return _responseHandler.BadRequest<VerificationFlowResponse>(
                "An active reset code already exists. Please use the existing code or wait for it to expire."
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, "Database error during Reset Password stage 1");


            return _responseHandler.InternalServerError<VerificationFlowResponse>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
    }

    /// <summary>
    /// Helper method to create consistent success responses
    /// </summary>
    private Response<VerificationFlowResponse> _GetSuccessResponse(string token, DateTime expiresAt)
    {
        return _responseHandler.Success(
            new VerificationFlowResponse
            {
                Token = token,
                ExpiresAt = expiresAt
            }
        );
    }

}
