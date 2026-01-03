using Domain.Entities;

namespace Application.Features.AuthFeature;

public class ValidationOtpResuult
{
    public Otp? Otp { get; set; }
    public bool IsExceededMaxAttempts { get; set; }
    public bool IsValid => Otp != null;


}

