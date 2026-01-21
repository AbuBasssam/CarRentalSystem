using Application.Models;

namespace Interfaces;

/// <summary>
/// Service interface for handling email communications.
/// </summary>
public interface IEmailService : IScopedService
{
    /// <summary>
    /// Sends a generic email message.
    /// </summary>
    Task<Result<bool>> SendEmailAsync(string toEmail, string body, string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the email confirmation message containing an OTP code.
    /// </summary>
    Task<Result<bool>> SendConfirmEmailMessage(string email, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the password reset message containing an OTP code.
    /// </summary>
    Task<Result<bool>> SendResetPasswordMessage(string email, string otpCode, CancellationToken cancellationToken = default);
}

