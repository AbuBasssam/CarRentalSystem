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

public class SignUpCommandHandler : IRequestHandler<SignUpCommand, Response<string>>
{
    #region Field(s)

    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;

    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;

    private readonly IMapper _mapper;

    private readonly ResponseHandler _responseHandler;
    private readonly IStringLocalizer<SharedResources> _localizer;

    #endregion

    #region Constructure(s)

    public SignUpCommandHandler(IUserService userService, IAuthService authService, IMapper mapper, ResponseHandler responseHandler, IStringLocalizer<SharedResources> localizer, IRefreshTokenRepository refreshTokenRepo, IUnitOfWork unitOfWork, IOtpService otpService)
    {
        _userService = userService;
        _authService = authService;

        _mapper = mapper;
        _responseHandler = responseHandler;
        _localizer = localizer;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
    }

    #endregion

    #region Handler(s)
    public async Task<Response<string>> Handle(SignUpCommand request, CancellationToken cancellationToken)
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
                    return _responseHandler.BadRequest<string>(string.Join('\n', createResult.Errors));
                }

                user = createResult.Data;

                // AssignDefaultRole
                await _userService.AddToRoleAsync(user, Roles.Customer);


                await _unitOfWork.SaveChangesAsync();
            }

            // ========== Case 2 : UnConfirmed User ==========
            else if (!existingUser.EmailConfirmed)
            {

                user = existingUser;


                await _refreshTokenRepo.RevokeUserTokenAsync(
                    user.Id,
                    enTokenType.VerificationToken
                );
                await _unitOfWork.SaveChangesAsync();
            }

            // ========== Case 3 : Existing User ==========
            else
            {
                await transaction.RollbackAsync();
                return _responseHandler.BadRequest<string>(
                    _localizer[SharedResourcesKeys.EmailAlreadyExists]
                );
            }

            var otpResult = await _otpService.GenerateAndSendOtpAsync
            (
                user.Id,
                request.Dto.Email,
                enOtpType.ConfirmEmail,
                5
            );

            if (!otpResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return _responseHandler.Success(string.Empty);
            }

            var verificationToken = _authService.GenerateVerificationToken(user, 15);
            await _refreshTokenRepo.AddAsync(verificationToken.refreshToken);


            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return _responseHandler.Success(verificationToken.AccessToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Error during sign-up for {Email}", request.Dto.Email);
            return _responseHandler.BadRequest<string>(_localizer[SharedResourcesKeys.UnexpectedError]);
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

