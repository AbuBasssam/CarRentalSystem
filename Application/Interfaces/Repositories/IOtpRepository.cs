using Domain.Entities;
using Domain.Enums;

namespace Interfaces;

public interface IOtpRepository : IGenericRepository<Otp, int>
{
    /// <summary>
    /// Get latest OTPs for a user of specific type, ordered by creation time descending
    /// </summary>
    IQueryable<Otp> GetLatestValidOtpAsync(int userId, enOtpType otpType);
    /// <summary>
    /// Get OTP by TokenJti for password reset flow tracking
    /// </summary>
    IQueryable<Otp> GetByTokenJtiAsync(string tokenJti, enOtpType otpType);

    /// <summary>
    /// Check if user has any active (non-expired, non-used) OTP of given type
    /// </summary>
    Task<bool> HasActiveOtpAsync(int userId, enOtpType otpType);

    /// <summary>
    /// Force expire all active OTPs for a user of specific type
    /// </summary>
    Task<int> ExpireAllActiveOtpsAsync(int userId, enOtpType otpType);
}