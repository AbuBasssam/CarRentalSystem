using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class ResendVerificationCodeHandler : IRequestHandler<ResendVerificationCodeCommand, Response<VerificationFlowResponse>>
{
    #region Field(s)
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
    private const int OTP_VALIDITY_MINUTES = 5;
    private const int TOKEN_VALIDITY_MINUTES = 15;
    #endregion

    #region Constructor(s)
    public ResendVerificationCodeHandler(
    IAuthService authService, IRefreshTokenRepository refreshTokenRepository, IUserService userService,
    IOtpService otpService, IEmailService emailService, IOtpRepository otpRepo,
    IUnitOfWork unitOfWork, IRequestContext context, IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler
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
        _emailService = emailService;
    }

    #endregion

    #region Handler(s)
    public async Task<Response<VerificationFlowResponse>> Handle(ResendVerificationCodeCommand request, CancellationToken cancellationToken)
    {


        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _userService.GetUserByEmailAsync(request.DTO.Email).FirstOrDefaultAsync();

            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.Unauthorized<VerificationFlowResponse>();
            }

            if (user.EmailConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.EmailAlreadyVerified]);
            }

            var regenerationResult = await _otpService.RegenerateOtpAsync(user.Id, enOtpType.ConfirmEmail, OTP_VALIDITY_MINUTES, cancellationToken);
            if (!regenerationResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.InternalServerError<VerificationFlowResponse>();
            }
            // Step 6: Revoke old reset token

            await _RevokeOldTokenAsync(_context.UserId ?? 0);

            // Step 7: Generate new reset token (stage 1: AwaitingVerification)

            var newToken = _authService.GenerateVerificationToken(
                user,
                TOKEN_VALIDITY_MINUTES

            );

            await _refreshTokenRepo.AddAsync(newToken.refreshToken);


            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);


            string otpCode = regenerationResult.Data;

            var emailSendResult = await _emailService.SendConfirmEmailMessage(user.Email!, otpCode);

            if (!emailSendResult.IsSuccess)
            {
                Log.Error($"Failed to send resend reset code email to user {_context.UserId}");
                return _GetFailurResponse();

            }
            var response = _GetSuccessResponse(newToken.AccessToken, (DateTime)newToken.refreshToken.ExpiryDate!);

            return response;


        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, "Unique constraint violation during Resend Verification");

                return _responseHandler.BadRequest<VerificationFlowResponse>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(dex, "Database error during email confirmation Resend code");
            return _responseHandler.BadRequest<VerificationFlowResponse>(
                _localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Error in Resend confirm Email otp Process");
            return _responseHandler.InternalServerError<VerificationFlowResponse>("Error Occurred");
        }

    }

    #endregion

    /// <summary>
    /// Revoke old reset token by JTI
    /// </summary>
    private async Task _RevokeOldTokenAsync(int userId)
    {
        var isTokenFound = await _refreshTokenRepo.RevokeUserTokenAsync(userId, enTokenType.VerificationToken);

        if (isTokenFound)
        {
            Log.Information($"Revoked verification token for user Id: {userId}");
        }
        else
        {
            Log.Warning($"No verification token found to revoke with user Id: {userId}");
        }
    }
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

    /// <summary>
    /// Creates consistent failur response for password reset flow
    /// </summary>
    /// <returns>failur response with verification flow data</returns>
    private Response<VerificationFlowResponse> _GetFailurResponse()
    {
        return _responseHandler.Success(
            new VerificationFlowResponse
            {
                Token = string.Empty,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_VALIDITY_MINUTES)
            }
        );
    }


}

