using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Application.Features.AuthFeature;
/* Old version
public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, Response<bool>>
{
    #region Field(s)

    private readonly IAuthService _authService;
    private readonly ResponseHandler _responseHandler;

    #endregion


    #region Constructor(s)

    public ConfirmEmailHandler(IAuthService authService, ResponseHandler responseHandler)
    {
        _authService = authService;
        _responseHandler = responseHandler;
    }

    #endregion


    #region Handler(s)
    public async Task<Response<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var confirmationResult = await _authService.ConfirmEmail(request.VerificationToken, request.ConfirmationCode);

        return confirmationResult.IsSuccess ?
            _responseHandler.Success(true) :
            _responseHandler.BadRequest<bool>(string.Join(',', confirmationResult.Errors));
    }
    #endregion
}*/



public class ConfirmEmailCommandHandler
    : IRequestHandler<ConfirmEmailCommand, Response<bool>>
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IOtpRepository _otpRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _context;
    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        IUserService userService,
        IAuthService authService,
        IOtpRepository otpRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork unitOfWork,
        IRequestContext context,
        ResponseHandler responseHandler,
        IStringLocalizer<SharedResources> localizer,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _userService = userService;
        _authService = authService;
        _otpRepo = otpRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _context = context;
        _responseHandler = responseHandler;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Response<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ========= 1. Validate Verification Token =========
            var token = _context.AuthToken;

            var tokenValidation = await _authService.ValidateSessionToken(token!, enTokenType.VerificationToken);

            if (!tokenValidation.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _responseHandler.Unauthorized<bool>();

            }
            var userIdResult = _authService.GetUserIdFromSessionToken(token!);

            if (!userIdResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _responseHandler.NotFound<bool>();
            }
            var userId = userIdResult.Data;

            // ========= 2. Load User =========
            var user = await _userService
                .GetUserByIdAsync(userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _responseHandler.NotFound<bool>();
            }

            // ========= 3. Load ACTIVE OTP =========
            var otp = await _otpRepo
                .GetLatestValidOtpAsync(userId, enOtpType.ConfirmEmail)
                .FirstOrDefaultAsync(cancellationToken);


            // ========= 4. Validate OTP =========
            var otpValidation = _ValidateOtp(userId, request.dto.OtpCode, otp);

            if (!otpValidation.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.BadRequest<bool>(
                    string.Join('\n', otpValidation.Errors));
            }

            // ========= 5. Confirm Email =========
            user.EmailConfirmed = true;
            user.EmailConfirmedAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user);

            // ========= 6. Consume OTP =========
            otp!.MarkAsUsed();
            otp.ForceExpire();
            _otpRepo.Update(otp);

            // ========= 7. Revoke Verification Token =========
            await _refreshTokenRepo
                .RevokeUserTokenAsync(userId, enTokenType.VerificationToken);

            // ========= 8. Commit =========
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);

            // التحقق مما إذا كان الخطأ هو Unique Constraint Violation
            // SQL Server Error Number: 2627 or 2601
            if (dex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                Log.Warning(dex, "Attempted to confirm email that is already confirmed or duplicate transaction for User");//: {UserId}, UserId
                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            Log.Error(dex, "Database update error during email confirmation");
            return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, "Error confirming email");
            return _responseHandler.BadRequest<bool>(
                _localizer[SharedResourcesKeys.UnexpectedError]);
        }
    }

    #region Helper Methods

    private Result<Otp> _ValidateOtp(int userId, string otpCode, Otp? otp)
    {


        if (otp == null)
        {
            return Result<Otp>.Failure([
                _localizer[SharedResourcesKeys.NotFound]
            ]);
        }

        if (otp.IsExpired)
        {
            return Result<Otp>.Failure([
                _localizer[SharedResourcesKeys.InvalidExpiredCode]
            ]);
        }

        // Verify OTP code
        var hashedCode = Helpers.HashString(otpCode);
        if (otp.Code != hashedCode)
        {
            return Result<Otp>.Failure([
                _localizer[SharedResourcesKeys.InvalidExpiredCode]
            ]);
        }

        return Result<Otp>.Success(otp);
    }

    #endregion
}

