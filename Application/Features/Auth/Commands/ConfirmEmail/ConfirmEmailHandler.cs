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
    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;

    #endregion

    #region Constructor(s)
    public ConfirmEmailCommandHandler(
    IUserService userService, IAuthService authService, IOtpService otpService,
    IOtpRepository otpRepo, IRefreshTokenRepository refreshTokenRepo, IUnitOfWork unitOfWork,
    IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler
    )
    {
        _userService = userService;
        _authService = authService;
        _otpRepo = otpRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
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



            // ========= 2. Load User =========
            var user = await _userService
                .GetUserByEmailAsync(request.dto.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await Task.Delay(Random.Shared.Next(100, 300));

                Log.Warning($"Email confirmation attempted for non-existent email: {request.dto.Email}");

                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            if (user.EmailConfirmed)
            {
                Log.Information($"User {user.Id} attempted to verify already verified account");
                return _responseHandler.Success(true);
            }



            // ========= 3. Validate OTP =========
            var otpValidation = await _otpService.ValidateOtp(user.Id, request.dto.OtpCode, enOtpType.ConfirmEmail, cancellationToken);

            if (!otpValidation.IsValid)
            {

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



            // ========= 6. Commit =========
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, $"Attempted to confirm email that is already confirmed or duplicate transaction {dex.Message} ");
                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }

            Log.Error(dex, $"Database update error during email confirmation{dex.Message}");

            return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(ex, $"Error in confirming email operation:{ex.Message}");
            return _responseHandler.BadRequest<bool>(
                _localizer[SharedResourcesKeys.UnexpectedError]);
        }
    }

    #endregion
}

