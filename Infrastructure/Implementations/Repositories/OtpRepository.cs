using Domain.Entities;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Implementations;

public class OtpRepository : GenericRepository<Otp, int>, IOtpRepository
{
    public OtpRepository(AppDbContext context) : base(context)
    {
    }
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
    public IQueryable<Otp> GetLatestOtp(int userId, enOtpType otpType)
    {
        return _dbSet
            .Where(
            o => o.UserId == userId
            && o.Type == otpType
            ).OrderByDescending(o => o.CreationTime);

    }

    public async Task<bool> HasActiveOtpAsync(int userId, enOtpType otpType)
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .AnyAsync(o => o.UserId == userId
                        && o.Type == otpType
                        && !o.IsUsed
                        && o.ExpirationTime > now);
    }
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
    /// Gets expired OTPs for cleanup based on retention policy
    /// 
    /// Cleanup Logic:
    /// 1. Used OTPs (IsUsed=1): delete after retention period
    /// 2. Expired OTPs: delete after retention period from expiration
    /// 3. Very old OTPs: delete after max age regardless of status (safety)
    /// 
    /// This ensures:
    /// - Audit trail is preserved for the retention period
    /// - No OTP stays in database longer than max age
    /// - Database stays clean and performant
    /// </summary>
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

