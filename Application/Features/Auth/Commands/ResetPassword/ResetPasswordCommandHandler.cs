using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.Enums;
using Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Response<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserService _userService;
    private readonly IOtpRepository _otpRepo;
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _context;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;



    public ResetPasswordCommandHandler(IUserService userSerivce,
        IOtpRepository otpRepo,
        IRefreshTokenRepository tokenRepo,
        IUnitOfWork unitOfWork,
        IRequestContext requestContext,
        IStringLocalizer<SharedResources> localizer,
        ResponseHandler responseHandler,
        UserManager<User> userManager)
    {
        _userService = userSerivce;
        _otpRepo = otpRepo;
        _tokenRepo = tokenRepo;
        _unitOfWork = unitOfWork;
        _context = requestContext;
        _localizer = localizer;
        _responseHandler = responseHandler;
        _userManager = userManager;
    }

    public async Task<Response<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            int userId = (int)_context.UserId!;
            var currentJti = _context.TokenJti;

            // ============================================================
            // Step 1: Find user (using UserId from token context)
            // ============================================================
            var user = await _userService
                .GetUserByIdAsync(userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                await transaction.RollbackAsync(cancellationToken);

                Log.Warning($"Password reset attempted for non-existent user: {userId}");

                return _responseHandler.BadRequest<bool>(
                    _localizer[SharedResourcesKeys.UserNotFound]
                );
            }

            // Step 2: Verify OTP exists and matches JTI from stage 2

            var otp = await _otpRepo.GetByTokenJti(currentJti!, enOtpType.ResetPassword).FirstOrDefaultAsync();

            if (otp == null)
            {
                await transaction.RollbackAsync(cancellationToken);

                Log.Warning($"No matching verified OTP found for user {userId}");

                return _responseHandler.BadRequest<bool>(
                    _localizer[SharedResourcesKeys.InvalidExpiredCode]
                );
            }



            // ============================================================
            // Step 5: Remove old password
            // ============================================================
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);

            if (!removePasswordResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                var errors = removePasswordResult.Errors
                    .Select(e => e.Description)
                    .ToList();
                string errMessage = string.Join(", ", errors);

                Log.Error($"Failed to remove old password for user {userId}: {errMessage}");

                return _responseHandler.BadRequest<bool>(errMessage);
            }

            // ============================================================
            // Step 6: Set new password
            // ============================================================
            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);

            if (!addPasswordResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);

                var errors = addPasswordResult.Errors
                    .Select(e => e.Description)
                    .ToList();
                string errMessage = string.Join(", ", errors);


                Log.Error($"Failed to add new password for user {userId}: {errMessage}");

                return _responseHandler.BadRequest<bool>(errMessage);
            }

            // ============================================================
            // Step 7: Invalidate OTP
            // ============================================================
            otp.ForceExpire();

            // ============================================================
            // Step 8: Revoke all existing auth tokens for security
            // ============================================================
            await _tokenRepo.RevokeUserTokenAsync(userId, enTokenType.AuthToken);

            // ============================================================
            // Step 9: Revoke current reset token
            // ============================================================
            await _RevokeCurrentResetTokenAsync(currentJti!);

            // ============================================================
            // Step 10: Update security stamp (invalidates existing sessions)
            // ============================================================
            var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);

            if (!securityStampResult.Succeeded)
            {
                Log.Warning($"Failed to update security stamp for user {userId}");
                // Continue anyway - not critical
            }

            // ============================================================
            // Step 11: Save all changes
            // ============================================================
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Log.Information($"Password successfully reset for user {userId}");

            return _responseHandler.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Error resetting password for user {UserId}", _context.UserId);
            return _responseHandler.InternalServerError<bool>("An error occurred while resetting your password.");
        }
    }
    /// <summary>
    /// Revoke current reset token by JTI
    /// </summary>
    private async Task _RevokeCurrentResetTokenAsync(string jti)
    {
        var isTokenFound = await _tokenRepo.RevokeUserTokenAsync(jti);

        if (isTokenFound)
        {
            Log.Information($"Revoked reset token with JTI {jti}");
        }
        else
        {
            Log.Warning($"No reset token found to revoke with JTI {jti}");
        }
    }
}
