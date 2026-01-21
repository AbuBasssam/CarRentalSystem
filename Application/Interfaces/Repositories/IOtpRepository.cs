using Domain.Entities;
using Domain.Enums;

namespace Interfaces;


/// <summary>
/// Repository interface for Otp operations.
/// Handles lifecycle management of otp codes.
/// </summary>
public interface IOtpRepository : IGenericRepository<Otp, int>
{
    /// <summary>
    /// Get latest OTP for verification attempt 
    /// Used in: ConfirmEmailHandler, VerifyResetCodeHandler
    /// </summary>
    IQueryable<Otp> GetLatestOtp(int userId, enOtpType otpType);

    /// <summary>
    /// Get latest VALID OTP for resend checks
    /// Used in: ResendVerificationCodeHandler, ResendResetCodeHandler
    /// </summary>
    IQueryable<Otp> GetLatestValidOtpAsync(int userId, enOtpType otpType);



    /// <summary>
    /// Check if user has any active (non-expired, non-used) OTP of given type
    /// </summary>
    Task<bool> HasActiveOtpAsync(int userId, enOtpType otpType);

    /// <summary>
    /// Force expire all active OTPs for a user of specific type
    /// </summary>
    Task<int> ExpireAllActiveOtpsAsync(int userId, enOtpType otpType);

    /// <summary>
    /// Gets expired OTPs that have exceeded the retention period for cleanup
    /// 
    /// OTP Lifecycle:
    /// - ConfirmEmail: 5 minutes validity
    /// - ResetPassword: 3 minutes validity
    /// - Used OTPs are marked with IsUsed = 1
    /// - Expired OTPs have ExpirationTime in the past
    /// 
    /// Cleanup Policy:
    /// Returns OTPs where:
    /// 1. IsUsed = 1 AND CreationTime older than retention period, OR
    /// 2. ExpirationTime older than retention period, OR
    /// 3. CreationTime older than max age (safety fallback)
    /// </summary>
    /// <param name="retentionHours">Hours to keep OTPs after use/expiry (default: 1)</param>
    /// <param name="maxAgeHours">Maximum age in hours for any OTP (default: 24)</param>
    /// <param name="batchSize">Maximum number of OTPs to return in one batch</param>
    /// <returns>List of expired OTPs ready for deletion</returns>
    Task<List<Otp>> GetExpiredOtpsForCleanupAsync(int retentionHours, int maxAgeHours, int batchSize);

}