using Application.Models;
using Domain.HelperClasses;
using Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Serilog;


namespace Application.Services;

public class EmailsService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailsService(EmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
    }

    public async Task<Result<bool>> SendEmailAsync(
     string toEmail,
     string body,
     string subject,
     CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await client.AuthenticateAsync(
                _emailSettings.FromEmail,
                _emailSettings.Password,
                cancellationToken);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Rento", _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = body,
                TextBody = body
            }.ToMessageBody();

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (SmtpCommandException ex)
        {
            Log.Error(ex,
                "SMTP command failed while sending email to {Email}", toEmail);

            return Result<bool>.Failure([
                "Email service rejected the command."
            ]);
        }
        catch (SmtpProtocolException ex)
        {
            Log.Error(ex,
                "SMTP protocol error while sending email to {Email}", toEmail);

            return Result<bool>.Failure([
                "Email service protocol error."
            ]);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Unexpected error while sending email to {Email}", toEmail);

            return Result<bool>.Failure([
                "Unexpected email service error."
            ]);
        }
    }

}


