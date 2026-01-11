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
/// Handles initial password reset request and sends verification code
/// </summary>
public class SendResetCodeCommandHandler : IRequestHandler<SendResetCodeCommand, Response<VerificationFlowResponse>>
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IAuthService _authServic;
    private readonly IOtpService _otpService;
    private readonly IOtpRepository _otpRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;

    private const int OTP_VALIDITY_MINUTES = 3;
    private const int TOKEN_VALIDITY_MINUTES = 15;

    #endregion

    #region Constructor(s)

    /// <summary>
    /// Initializes a new instance of SendResetCodeCommandHandler
    /// </summary>
    public SendResetCodeCommandHandler(
        IEmailService emailService,
        IUserService userService,
        IAuthService authServic,
        IOtpRepository otpRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork unitOfWork,
        IStringLocalizer<SharedResources> localizer,
        ResponseHandler responseHandler,
        IOtpService otpService)
    {
        _emailService = emailService;
        _userService = userService;
        _authServic = authServic;
        _otpRepo = otpRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
        _localizer = localizer;
        _otpService = otpService;
    }

    #endregion

    #region Handler(s)
    /// <summary>
    /// Processes password reset initiation request
    /// </summary>
    /// <param name="request">Send reset code command containing user email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing verification token</returns>
    public async Task<Response<VerificationFlowResponse>> Handle(SendResetCodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        var email = request.DTO.Email;

        try
        {
            // Step 1: Find user by email 
            var user = await _userService
                .GetUserByEmailAsync(email)
                .FirstOrDefaultAsync();

            if (user == null)
            {

                // Security: Always return success to prevent email enumeration
                Log.Warning($"Password reset requested for non-existent email: {email}");

                return _GetSuccessResponse(string.Empty, DateTime.UtcNow.AddMinutes(TOKEN_VALIDITY_MINUTES));

            }

            // Step 2: Check for existing active reset codes
            var existingOtp = _otpRepo
                .GetLatestValidOtpAsync(user.Id, enOtpType.ResetPassword)
                .FirstOrDefault();

            if (existingOtp != null)
            {
                // Force expire existing OTP
                existingOtp.ForceExpire();
                Log.Information($"Expired existing reset code for user {user.Id}");
            }


            // Step 3: Generate new OTP code

            Result<(string otpCode, string jti)> result = await _otpService.GenerateOtpWithJtiAsync(
                user.Id,
                enOtpType.ResetPassword,
                OTP_VALIDITY_MINUTES,
                cancellationToken
            );

            if (!result.IsSuccess)
            {
                return _GetSuccessResponse(string.Empty, DateTime.UtcNow.AddMinutes(TOKEN_VALIDITY_MINUTES));


            }

            // Step 4: Generate JWT token for stage 1 (AwaitingVerification)
            var token = _authServic.GenerateResetToken(user, TOKEN_VALIDITY_MINUTES, result.Data.jti, enResetPasswordStage.AwaitingVerification);


            // Step 5: Save to DB 

            await _refreshTokenRepo.AddAsync(token.refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            Log.Information($"OTP and token saved for user {user.Id}. Attempting to send reset password email.");


            // Step 5: Send email (don't save OTP if email fails)
            var emailResult = await _emailService.SendResetPasswordMessage(user.Email!, result.Data.otpCode);

            if (!emailResult.IsSuccess)
            {
                return _GetSuccessResponse(string.Empty, DateTime.UtcNow.AddMinutes(TOKEN_VALIDITY_MINUTES));


            }

            Log.Information($"Password reset code sent successfully to user {user.Id}");

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

    #endregion

    #region Helper Method(s)
    /// <summary>
    /// Creates consistent success response for password reset flow
    /// </summary>
    /// <param name="token">Verification token</param>
    /// <param name="expiresAt">Token expiration timestamp</param>
    /// <returns>Success response with verification flow data</returns>
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

    #endregion
}
