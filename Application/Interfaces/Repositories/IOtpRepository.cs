using Domain.Entities;
using Domain.Enums;

namespace Interfaces;

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
}