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
        return _dbSet
            .Where(o => o.User.Id == userId && o.Type == otpType)
            .OrderByDescending(o => o.CreationTime);

    }
    public async Task<Otp?> GetByTokenJtiAsync(string tokenJti, enOtpType otpType)
    {
        return await _dbSet
            .FirstOrDefaultAsync(o => o.TokenJti == tokenJti && o.Type == otpType);
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
            );
    }
}

