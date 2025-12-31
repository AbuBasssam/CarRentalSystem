using Application.Models;

namespace Interfaces;

public interface IEmailService : IScopedService
{
    Task<Result<bool>> SendEmailAsync(
      string toEmail,
      string body,
      string subject,
      CancellationToken cancellationToken = default);
}

