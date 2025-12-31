using Domain.Entities;
using Domain.Enums;

namespace Interfaces;

public interface IOtpRepository : IGenericRepository<Otp, int>
{
    IQueryable<Otp> GetLatestValidOtpAsync(int userId, enOtpType otpType);

}