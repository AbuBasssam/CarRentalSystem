using Application.Models;
using ApplicationLayer.Resources;
using AutoMapper;
using Domain.AppMetaData;
using Domain.Entities;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class SignUpCommandHandler : IRequestHandler<SignUpCommand, Response<bool>>
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;

    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;

    private readonly IMapper _mapper;

    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;

    #endregion

    #region Constructure(s)

    public SignUpCommandHandler(IUserService userService, IAuthService authService, IMapper mapper, ResponseHandler responseHandler, IStringLocalizer<SharedResources> localizer, IRefreshTokenRepository refreshTokenRepo, IUnitOfWork unitOfWork, IOtpService otpService, IEmailService emailService)
    {
        _userService = userService;
        _authService = authService;

        _mapper = mapper;
        _responseHandler = responseHandler;
        _localizer = localizer;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _emailService = emailService;
    }

    #endregion

    #region Handler(s)
    public async Task<Response<bool>> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {

        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // ==========Step 1: Check user existing ==========
            var existingUser = await _userService
                .GetUserByEmailAsync(request.Dto.Email)
                .FirstOrDefaultAsync();

            User user;

            // ========== Case 1: Create New User ==========
            if (existingUser == null)
            {
                var newUser = _mapper.Map<User>(request.Dto);
                var createResult = await _CreateUser(newUser, request.Dto.Password);

                if (!createResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return _responseHandler.BadRequest<bool>(string.Join('\n', createResult.Errors));
                }

                user = createResult.Data;

                // Assign To Customer Role
                await _userService.AddToRoleAsync(user, Roles.Customer);


                await _unitOfWork.SaveChangesAsync();
            }

            // ========== Case 2 : UnConfirmed User ==========
            else if (!existingUser.EmailConfirmed)
            {
                user = existingUser;

                Log.Information("Re-registration attempt for unconfirmed user: {UserId}", user.Id);

                // Force Expire for Old Active Confirm email otps
                await _otpService.ExpireActiveOtpAsync(user.Id, enOtpType.ConfirmEmail, cancellationToken);


                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }


            // ========== Case 3 : Existing User ==========
            else
            {
                Log.Warning($"Registration attempt with confirmed email: {request.Dto.Email}");
                await transaction.RollbackAsync();
                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.EmailAlreadyExists]);
            }

            var generateOtpResult = await _otpService.GenerateOtpAsync(user.Id, enOtpType.ConfirmEmail, 5);

            if (!generateOtpResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Error($"Failed to generate OTP for user {user.Id}: {string.Join(", ", generateOtpResult.Errors)}");
                return _responseHandler.InternalServerError<bool>();
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();



            var sendingEmailResult = await _emailService.SendConfirmEmailMessage(user.Email!, generateOtpResult.Data);

            return _responseHandler.Created(true);
        }
        catch (DbUpdateException dex)
        {
            await transaction.RollbackAsync(cancellationToken);


            if (dex.IsUniqueConstraintViolation())
            {
                Log.Warning(dex, "Unique constraint violation during sign-up");

                return _responseHandler.BadRequest<bool>(_localizer[SharedResourcesKeys.EmailAlreadyExists]);
            }

            Log.Error(dex, "Database error during sign-up");
            return _responseHandler.BadRequest<bool>(
                _localizer[SharedResourcesKeys.UnexpectedError]);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, $"Error during sign-up for {request.Dto.Email}:{ex.Message}");
            return _responseHandler.InternalServerError<bool>(_localizer[SharedResourcesKeys.UnexpectedError]);
        }
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates new user account
    /// </summary>
    private async Task<Result<User>> _CreateUser(User user, string password)
    {
        var result = await _userService.CreateUserAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<User>.Failure(errors);
        }

        return Result<User>.Success(user);
    }

    #endregion
}

