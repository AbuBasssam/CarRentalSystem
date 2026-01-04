using Application.Models;

namespace Interfaces;

public interface IEmailService : IScopedService
{
    Task<Result<bool>> SendEmailAsync(
      string toEmail,
      string body,
      string subject,
      CancellationToken cancellationToken = default);
    Task<Result<bool>> SendConfirmEmailMessage(string email, string otpCode, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendResetPasswordMessage(string email, string otpCode, CancellationToken cancellationToken = default);

}

