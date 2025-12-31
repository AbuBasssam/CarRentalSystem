using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Interfaces;


namespace Application.Services;

public class OtpService : IOtpService
{
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;

    public OtpService(IEmailService emailService, IOtpRepository otpRepo, IUnitOfWork unitOfWork)
    {
        _emailService = emailService;
        _otpRepo = otpRepo;
    }

    public async Task<Result<bool>> GenerateAndSendOtpAsync(int userId, string email, enOtpType otpType, int validityMinutes)
    {

        var otpCode = _GenerateOtp();

        var subject = otpType == enOtpType.ConfirmEmail
            ? "Confirm Email"
            : "Reset Password";

        var message = otpType == enOtpType.ConfirmEmail
            ? $"Your code to confirm your email is: {otpCode}"
            : $"Your code to reset your password is: {otpCode}";


        var sendResult = await _emailService.SendEmailAsync(email, message, subject);

        if (!sendResult.IsSuccess)
            return Result<bool>.Failure(sendResult.Errors);


        await _SaveOtpToDb(userId, otpCode, otpType, validityMinutes);

        return Result<bool>.Success(true);
    }

    private string _GenerateOtp()
    {
        Random generator = new Random();
        return generator.Next(100000, 1000000).ToString("D6");
    }
    private async Task _SaveOtpToDb(int UserID, string otp, enOtpType otpType, int minutesValidDuration)
    {
        var validFor = TimeSpan.FromMinutes(minutesValidDuration);
        var otpEntity = new Otp(Helpers.HashString(otp), otpType, UserID, validFor);


        await _otpRepo.AddAsync(otpEntity);


    }
}
