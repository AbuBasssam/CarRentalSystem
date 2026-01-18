using Application.Features.AuthFeature;
using Application.Models;
using Domain.Enums;

namespace Interfaces;

public interface IOtpService : IScopedService
{
    /// <summary>
    /// Generates OTP to user
    /// </summary>
    Task<Result<string>> GenerateOtpAsync(int userId, enOtpType otpType, int expirationMinutes, CancellationToken cancellationToken = default);


    /// <summary>
    /// Expires and marks old active OTP as used before creating new one
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="otpType">Type of OTP to expire</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if old OTP was found and expired, false if no active OTP existed</returns>
    Task<bool> ExpireActiveOtpAsync(
        int userId,
        enOtpType otpType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Combines ExpireActiveOtpAsync + GenerateAndSendOtpAsync in one atomic operation
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="otpType">Type of OTP</param>
    /// <param name="expirationMinutes">Expiration time in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<string>> RegenerateOtpAsync(int userId, enOtpType otpType, int expirationMinutes, CancellationToken cancellationToken = default);

    Task<ValidationOtpResuult> ValidateOtp(int userId, string otpCode, enOtpType enOtpType, CancellationToken ct = default);

    /// <summary>
    /// Check if user can resend OTP based on cooldown period
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="otpType">OTP type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if resend is allowed and remaining time if not</returns>
    Task<(bool canResend, TimeSpan? remaining)> CanResendOtpAsync(int userId, enOtpType otpType, CancellationToken cancellationToken = default);
}

