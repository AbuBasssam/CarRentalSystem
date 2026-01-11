using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;
public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Response<bool>>
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly IOtpRepository _otpRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _context;
    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;

    #endregion

    #region Constructor(s)
    public ConfirmEmailCommandHandler(
    IUserService userService, IAuthService authService, IOtpService otpService,
    IOtpRepository otpRepo, IRefreshTokenRepository refreshTokenRepo, IUnitOfWork unitOfWork,
    IRequestContext context, IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler
    )
    {
        _userService = userService;
        _authService = authService;
        _otpRepo = otpRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _context = context;
        _responseHandler = responseHandler;
        _localizer = localizer;
        _otpService = otpService;
    }

    #endregion

    #region Handler(s)
    public async Task<Response<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ========= 1. Validate Verification Token =========
            var token = _context.AuthToken;

            var isValidToken = await _authService.ValidateSessionToken(token!, enTokenType.VerificationToken);

            if (!isValidToken)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _responseHandler.Unauthorized<bool>();

            }
            var userIdResult = _authService.GetUserIdFromSessionToken(token!);

            if (!userIdResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _responseHandler.Unauthorized<bool>();
            }
            var userId = userIdResult.Data;



            // ========= 2. Load User =========
            var user = await _userService
                .GetUserByIdAsync(userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return _responseHandler.Unauthorized<bool>();
            }



            // ========= 3. Validate OTP =========
            var otpValidation = await _otpService.ValidateOtp(userId, request.dto.OtpCode, enOtpType.ConfirmEmail, cancellationToken);

            if (!otpValidation.IsValid)
            {

                if (otpValidation.IsExceededMaxAttempts)

                    await _refreshTokenRepo.RevokeUserTokenAsync(userId, enTokenType.VerificationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            // ========= 4. Confirm Email =========
            user.EmailConfirmed = true;
            user.EmailConfirmedAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user);



            // ========= 5. Consume OTP =========
            var otp = otpValidation.Otp!;

            otp.ForceExpire();

            // ========= 6. Revoke Verification Token =========
            await _refreshTokenRepo
                .RevokeUserTokenAsync(userId, enTokenType.VerificationToken);

            // ========= 7. Commit =========
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, "Attempted to confirm email that is already confirmed or duplicate transaction ");
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

    #endregion
}

