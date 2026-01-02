using Application.Models;
using Domain.Enums;

namespace Interfaces;

public interface IOtpService : IScopedService
{
    /// <summary>
    /// Generates and sends OTP to user
    /// </summary>
    Task<Result<bool>> SendOtpEmailAsync(
        int userId,
        string email,
        enOtpType otpType,
        int validityMinutes,
         CancellationToken ct = default
    );
    /// <summary>
    /// Expires and marks old active OTP as used before creating new one
    /// Prevents Unique Constraint Violation on active OTPs
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
    /// Recommended for preventing race conditions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="otpType">Type of OTP</param>
    /// <param name="expirationMinutes">Expiration time in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> RegenerateOtpAsync(
        int userId,
        string email,
        enOtpType otpType,
        int expirationMinutes,
        CancellationToken cancellationToken = default);
}

