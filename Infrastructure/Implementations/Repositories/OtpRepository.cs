using Domain.Entities;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Implementations;
/// <summary>
/// Repository implementation for Otp entity operations.
/// Provides specialized methods for managing One-Time Password lifecycles, 
/// including validation, retrieval, and cleanup.
/// </summary>
public class OtpRepository : GenericRepository<Otp, int>, IOtpRepository
{
    public OtpRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves the most recent valid and unused OTP for a user.
    /// Checks for expiration, usage status, and maximum failed attempts.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="otpType">The type of OTP (e.g., Email Confirmation, Password Reset).</param>
    /// <returns>An IQueryable containing the latest valid OTP.</returns>
    public IQueryable<Otp> GetLatestValidOtpAsync(int userId, enOtpType otpType)
    {
        const byte MAX_ATTEMPTS = 5;

        return _dbSet
            .Where(o =>
                o.UserId == userId
                && o.Type == otpType
                && o.ExpirationTime > DateTime.UtcNow
                && !o.IsUsed
                && o.AttemptsCount < MAX_ATTEMPTS
            )
            .OrderByDescending(o => o.CreationTime);
    }

    /// <summary>
    /// Retrieves the most recent OTP record for a specific user and type, 
    /// regardless of whether it is used or expired.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="otpType">The category of the OTP (e.g., EmailConfirmation, ResetPassword).</param>
    /// <returns>An IQueryable containing the latest OTP record.</returns>
    public IQueryable<Otp> GetLatestOtp(int userId, enOtpType otpType)
    {
        return _dbSet
            .Where(
            o => o.UserId == userId
            && o.Type == otpType
            ).OrderByDescending(o => o.CreationTime);

    }

    /// <summary>
    /// Checks if there is any active (non-expired and unused) OTP for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="otpType">The type of OTP.</param>
    /// <returns>True if an active OTP exists; otherwise, false.</returns>
    public async Task<bool> HasActiveOtpAsync(int userId, enOtpType otpType)
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .AnyAsync(o => o.UserId == userId
                        && o.Type == otpType
                        && !o.IsUsed
                        && o.ExpirationTime > now);
    }

    /// <summary>
    /// Forcefully expires all currently active and valid OTPs for a specific user and type.
    /// Usually called before generating a new OTP to ensure only one is active at a time.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="otpType">The category of the OTP to be expired.</param>
    /// <returns>The number of OTP records affected by the operation.</returns>
    public async Task<int> ExpireAllActiveOtpsAsync(int userId, enOtpType otpType)
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .Where(o => o.UserId == userId
                     && o.Type == otpType
                     && !o.IsUsed
                     && o.ExpirationTime > now)
            .ExecuteUpdateAsync(
                setters => setters
                .SetProperty(o => o.ExpirationTime, now)
                .SetProperty(o => o.IsUsed, true)
            );
    }

    /// <summary>
    /// Retrieves a list of OTP records that are eligible for cleanup based on the system's retention policy.
    /// </summary>
    /// <remarks>
    /// The cleanup logic follows three main conditions:
    /// 1. Used OTPs: Eligible for deletion after the specified retention period from their creation.
    /// 2. Expired OTPs: Eligible for deletion after the specified retention period from their expiration time.
    /// 3. Safety Fallback: Any OTP exceeding the maximum age limit is deleted regardless of its status.
    /// </remarks>
    /// <param name="retentionHours">The number of hours to retain OTPs before they become eligible for deletion.</param>
    /// <param name="maxAgeHours">The absolute maximum age of an OTP record before it is forcefully removed.</param>
    /// <param name="batchSize">The maximum number of records to retrieve in a single operation to prevent memory overhead.</param>
    /// <returns>A list of OTP entities identified for permanent removal from the database.</returns>
    public async Task<List<Otp>> GetExpiredOtpsForCleanupAsync(int retentionHours, int maxAgeHours, int batchSize)
    {
        var now = DateTime.UtcNow;
        var retentionCutoff = now.AddHours(-retentionHours);
        var maxAgeCutoff = now.AddHours(-maxAgeHours);

        var expiredOtps = await _dbSet
            .Where(o =>
                // Condition 1: Used OTPs older than retention period
                (o.IsUsed && o.CreationTime < retentionCutoff) ||

                // Condition 2: Expired OTPs older than retention period
                (o.ExpirationTime < retentionCutoff) ||

                // Condition 3: Any OTP older than max age (safety fallback)
                (o.CreationTime < maxAgeCutoff)
            )
            .OrderBy(o => o.CreationTime)
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync();

        return expiredOtps;
    }
}

