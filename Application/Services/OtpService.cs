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
    public async Task<Result<(string otp, string jti)>> GenerateOtpWithJtiAsync(int userId, enOtpType otpType, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        var otpCode = Helpers.GenerateOtp();
        var jti = Guid.NewGuid().ToString();

        await _SaveOtpToDb(userId, otpCode, otpType, expirationMinutes, jti);

        return Result<(string, string)>.Success((otpCode, jti));
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
    public async Task<ValidationOtpResuult> ValidateOtp(string tokenJti, string otpCode, enOtpType enOtpType, CancellationToken ct = default)
    {
        var otp = await _otpRepo.GetByTokenJti(tokenJti, enOtpType).FirstOrDefaultAsync(ct);
        if (otp == null)
        {
            Log.Warning(
            $"OTP validation failed: no active OTP found With jti {tokenJti}");

            return new ValidationOtpResuult();

        }

        return _OtpValidationResult(otp, otpCode);
    }
    public async Task<ValidationOtpResuult> ValidateOtp(int userId, string otpCode, enOtpType enOtpType, CancellationToken ct = default)
    {
        var otp =
            await _otpRepo
                .GetLatestValidOtpAsync(userId, enOtpType)
                .FirstOrDefaultAsync(ct);
        if (otp == null)
        {
            Log.Warning(
            $"OTP validation failed: no active OTP found for UserId {userId}");

            return new ValidationOtpResuult();

        }

        return _OtpValidationResult(otp, otpCode);



    }

    private ValidationOtpResuult _OtpValidationResult(Otp otp, string originalCode)
    {
        var validtionResult = new ValidationOtpResuult();

        if (!otp.IsValidForVerification())
        {
            Log.Warning(
                $"OTP invalid state for UserId {otp.UserId}. Expired/Used/MaxAttempts");

            return validtionResult;
        }

        // Verify OTP code
        var hashedCode = Helpers.HashString(originalCode);
        if (otp.Code != hashedCode)
        {
            otp.IncrementAttempts();
            otp.UpdateLastAttempt();


            if (otp.HasExceededMaxAttempts())
            {
                otp.ForceExpire();

                Log.Warning($"OTP locked after max attempts for UserId {otp.UserId}");
                validtionResult.IsExceededMaxAttempts = true;
                return validtionResult;
            }
            else
                Log.Warning($"OTP validation failed: invalid code for UserId {otp.UserId}");

            return validtionResult;
        }
        validtionResult.Otp = otp;

        return validtionResult;
    }

    #endregion

    #region Helper Method(s)

    private async Task _SaveOtpToDb(int UserID, string otp, enOtpType otpType, int minutesValidDuration, string? jti = null)
    {
        var validFor = TimeSpan.FromMinutes(minutesValidDuration);
        var hashedOtp = Helpers.HashString(otp);
        var otpEntity = new Otp(hashedOtp, otpType, UserID, validFor, jti);


        await _otpRepo.AddAsync(otpEntity);


    }


    #endregion
}
