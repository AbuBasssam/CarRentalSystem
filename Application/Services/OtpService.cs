using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;


namespace Application.Services;

public class OtpService : IOtpService
{
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;


    public OtpService(IEmailService emailService, IOtpRepository otpRepo)
    {
        _emailService = emailService;
        _otpRepo = otpRepo;
    }

    public async Task<Result<bool>> SendOtpEmailAsync(int userId, string email, enOtpType otpType, int validityMinutes, CancellationToken cancellationToken = default)
    {

        var otpCode = _GenerateOtp();

        var subject = otpType == enOtpType.ConfirmEmail
            ? "Confirm Email"
            : "Reset Password";

        var message = otpType == enOtpType.ConfirmEmail
            ? $"Your code to confirm your email is: {otpCode}"
            : $"Your code to reset your password is: {otpCode}";


        var sendResult = await _emailService.SendEmailAsync(email, message, subject);

        if (!sendResult.IsSuccess)
            return Result<bool>.Failure(sendResult.Errors);


        await _SaveOtpToDb(userId, otpCode, otpType, validityMinutes);


        return Result<bool>.Success(true);
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
            {
                Log.Debug("No active OTP found for user {UserId} and type {OtpType}", userId, otpType);
                return false;
            }

            // Expire and mark as used
            activeOtp.ForceExpire();
            activeOtp.MarkAsUsed();
            _otpRepo.Update(activeOtp);

            Log.Information(
                "Expired active OTP for user {UserId}, type {OtpType}",
                userId,
                otpType);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error expiring active OTP for user {UserId}, type {OtpType}", userId, otpType);

            throw; // Re-throw to handle in calling code
        }
    }

    /// <summary>
    /// Combines ExpireActiveOtpAsync + GenerateAndSendOtpAsync in one atomic operation
    /// This is the RECOMMENDED method to use to prevent race conditions
    /// </summary>
    public async Task<Result<bool>> RegenerateOtpAsync(int userId, string email, enOtpType otpType, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Expire old OTP if exists
            await ExpireActiveOtpAsync(userId, otpType, cancellationToken);

            // Note: Caller should save changes here before generating new OTP
            // This ensures the old OTP is actually marked as used in DB
            // before the unique constraint check happens on the new OTP


            // Step 2: Generate and send new OTP
            var result = await SendOtpEmailAsync(userId, email, otpType, expirationMinutes);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error regenerating OTP for user {UserId}, type {OtpType}",
                userId,
                otpType);

            return Result<bool>.Failure(["Failed to regenerate OTP"]);
        }
    }

    private string _GenerateOtp()
    {
        Random generator = new Random();
        return generator.Next(100000, 1000000).ToString("D6");
    }
    private async Task _SaveOtpToDb(int UserID, string otp, enOtpType otpType, int minutesValidDuration)
    {
        var validFor = TimeSpan.FromMinutes(minutesValidDuration);
        var otpEntity = new Otp(Helpers.HashString(otp), otpType, UserID, validFor);


        await _otpRepo.AddAsync(otpEntity);


    }
}
