using Application.Features.AuthFeature;
using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;


namespace Application.Services;

public class OtpService : IOtpService
{
    #region Field(s)
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;
    private readonly IStringLocalizer<SharedResources> _localizer;
    #endregion

    #region Constructor(s)
    public OtpService(IEmailService emailService, IOtpRepository otpRepo, IStringLocalizer<SharedResources> localizer)
    {
        _emailService = emailService;
        _otpRepo = otpRepo;
        _localizer = localizer;
    }

    #endregion

    #region Method(s)
    public async Task<Result<string>> GenerateOtpAsync(int userId, enOtpType otpType, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        try
        {
            var otpCode = Helpers.GenerateOtp();

            await _SaveOtpToDb(userId, otpCode, otpType, expirationMinutes);

            return Result<string>.Success(otpCode);

        }
        catch (Exception ex)
        {

            Log.Error(ex, $"Error generating OTP for user {userId}, type {otpType}");
            return Result<string>.Failure(["Failed to generate OTP"]);
        }
    }

    /// <summary>
    /// Expires and marks old active OTP as used before creating new one
    /// Prevents Unique Constraint Violation on active OTPs
    /// </summary>
    public async Task<bool> ExpireActiveOtpAsync(int userId, enOtpType otpType, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeOtp = await _otpRepo
                .GetLatestValidOtpAsync(userId, otpType)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeOtp == null)
                return true;

            // Expire and mark as used
            activeOtp.ForceExpire();
            activeOtp.MarkAsUsed();

            Log.Information($"Expired active OTP for user {userId}, type {otpType}");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error expiring active OTP for user {userId}, type {otpType}");

            return false;
        }
    }

    /// <summary>
    /// Combines ExpireActiveOtpAsync + GenerateAndSendOtpAsync in one atomic operation
    /// </summary>
    public async Task<Result<string>> RegenerateOtpAsync(int userId, enOtpType otpType, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Expire old OTP if exists
            await ExpireActiveOtpAsync(userId, otpType, cancellationToken);

            // Step 2: Generate new OTP
            var otpCode = Helpers.GenerateOtp();


            await _SaveOtpToDb(userId, otpCode, otpType, expirationMinutes);

            return Result<string>.Success(otpCode);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                $"Error regenerating OTP for user {userId}, type {otpType}");

            return Result<string>.Failure(["Failed to regenerate OTP"]);
        }
    }

    /// <summary>
    /// Validates the OTP and manages attempt counting.
    /// </summary>
    public async Task<ValidationOtpResuult> ValidateOtp(int userId, string otpCode, enOtpType enOtpType, CancellationToken ct)
    {
        var validtionResult = new ValidationOtpResuult();
        var otp = await _otpRepo
                .GetLatestValidOtpAsync(userId, enOtpType)
                .FirstOrDefaultAsync(ct);


        if (otp == null)
        {
            Log.Warning(
            $"OTP validation failed: no active OTP found for UserId {userId}");

            return validtionResult;

        }
        if (!otp.IsValidForVerification())
        {
            Log.Warning(
                $"OTP invalid state for UserId {userId}. Expired/Used/MaxAttempts");

            return validtionResult;
        }

        // Verify OTP code
        var hashedCode = Helpers.HashString(otpCode);
        if (otp.Code != hashedCode)
        {
            otp.IncrementAttempts();

            if (otp.HasExceededMaxAttempts())
            {
                otp.ForceExpire();
                otp.MarkAsUsed();

                Log.Warning($"OTP locked after max attempts for UserId {userId}");
                validtionResult.IsExceededMaxAttempts = true;
                return validtionResult;
            }
            else
                Log.Warning($"OTP validation failed: invalid code for UserId {userId}");

            return validtionResult;
        }
        validtionResult.Otp = otp;

        return validtionResult;
    }

    #endregion

    #region Helper Method(s)

    private async Task _SaveOtpToDb(int UserID, string otp, enOtpType otpType, int minutesValidDuration)
    {
        var validFor = TimeSpan.FromMinutes(minutesValidDuration);
        var hashedOtp = Helpers.HashString(otp);
        var otpEntity = new Otp(hashedOtp, otpType, UserID, validFor);


        await _otpRepo.AddAsync(otpEntity);


    }

    #endregion
}
