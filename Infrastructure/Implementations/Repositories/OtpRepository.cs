using Domain.Entities;
using Domain.Enums;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;

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
}

