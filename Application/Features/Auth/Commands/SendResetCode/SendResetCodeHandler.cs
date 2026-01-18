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
public class SendResetCodeHandler : IRequestHandler<SendResetCodeCommand, Response<bool>>
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IAuthService _authServic;
    private readonly IOtpService _otpService;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;

    private const int OTP_VALIDITY_MINUTES = 3;

    #endregion

    #region Constructor(s)

    /// <summary>
    /// Initializes a new instance of SendResetCodeHandler
    /// </summary>
    public SendResetCodeHandler(
        IEmailService emailService, IUserService userService, IAuthService authServic,
        IOtpService otpService, IOtpRepository otpRepo, IUnitOfWork unitOfWork,
        IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler)
    {
        _emailService = emailService;
        _userService = userService;
        _authServic = authServic;
        _otpRepo = otpRepo;
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
    public async Task<Response<bool>> Handle(SendResetCodeCommand request, CancellationToken cancellationToken)
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
                await Task.Delay(Random.Shared.Next(100, 300));

                Log.Warning($"Password reset requested for non-existent email: {email}");

                return _responseHandler.Success(true);


            }

            if (!user.EmailConfirmed)
            {
                await Task.Delay(Random.Shared.Next(100, 300));
                Log.Warning(
                    "Password reset attempted for unconfirmed account with UserId: {UserId}",
                    user.Id
                );

                return _responseHandler.Success(true);
            }
            // Step 2: Generate reset otp

            var generateOtpResult = await _otpService.RegenerateOtpAsync(user.Id, enOtpType.ResetPassword, OTP_VALIDITY_MINUTES, cancellationToken);

            if (!generateOtpResult.IsSuccess)
            {
                Log.Error($"Failed to generate OTP for user {user.Id}: {string.Join(", ", generateOtpResult.Errors)}");
                return _responseHandler.InternalServerError<bool>();


            }



            // Step 3: Save to DB 

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            Log.Information($"OTP and token saved for user {user.Id}. Attempting to send reset password email.");


            // Step 4: Send email 

            await _emailService.SendResetPasswordMessage(user.Email!, generateOtpResult.Data);

            Log.Information($"Password reset code sent successfully to user {user.Id}");

            return _responseHandler.Success(true);
        }
        catch (DbUpdateException dex) when (dex.IsUniqueConstraintViolation())
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Warning($"Active OTP constraint violation for email {request.DTO.Email}. Possible race condition.");

            return _responseHandler.BadRequest<bool>(
                "An active reset code already exists. Please use the existing code or wait for it to expire."
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, $"Database error during Reset Password stage 1: {ex.Message}");


            return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
    }

    #endregion


}
