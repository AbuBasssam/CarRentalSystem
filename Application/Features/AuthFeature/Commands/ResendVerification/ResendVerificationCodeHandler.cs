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
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _context;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;
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
    public async Task<Response<string>> Handle(ResendVerificationCodeCommand request, CancellationToken cancellationToken)
    {

        var token = _context.AuthToken;
        var emailResult = _authService.GetEmailFromSessionToken(token!);

        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            string email = emailResult.Data!;
            var user = await _userService.GetUserByEmailAsync(email).FirstOrDefaultAsync();

            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.Unauthorized<string>();
            }

            if (user.EmailConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.BadRequest<string>(_localizer[SharedResourcesKeys.EmailAlreadyVerified]);
            }

            var regenerationResult = await _otpService.RegenerateOtpAsync(user.Id, enOtpType.ConfirmEmail, 5, cancellationToken);
            if (!regenerationResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _responseHandler.InternalServerError<string>();
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);


            string otpCode = regenerationResult.Data;

            await _emailService.SendConfirmEmailMessage(user.Email!, otpCode);

            return _responseHandler.Success(string.Empty);


        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, "Unique constraint violation during Resend Verification");

                return _responseHandler.BadRequest<string>(_localizer[SharedResourcesKeys.InvalidExpiredCode]);
            }
            await transaction.RollbackAsync(cancellationToken);

            Log.Error(dex, "Database error during email confirmation Resend code");
            return _responseHandler.BadRequest<string>(
                _localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Error in Resend confirm Email otp Process");
            return _responseHandler.InternalServerError<string>("Error Occurred");
        }

    }

    #endregion


}

