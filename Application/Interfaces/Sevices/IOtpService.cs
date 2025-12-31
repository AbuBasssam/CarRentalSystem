using Application.Models;
using Domain.Enums;

namespace Interfaces;

public interface IOtpService : IScopedService
{
    Task<Result<bool>> GenerateAndSendOtpAsync(
        int userId,
        string email,
        enOtpType otpType,
        int validityMinutes
    );
}

