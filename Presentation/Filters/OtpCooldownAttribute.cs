using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Filters;

/// <summary>
/// Attribute to enforce OTP cooldown period
/// Apply to endpoints that resend OTP codes
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OtpCooldownAttribute : TypeFilterAttribute
{
    public OtpCooldownAttribute(enOtpType otpType) : base(typeof(OtpCooldownFilter))
    {
        // Pass OtpType to filter via Arguments
        Arguments = new object[] { otpType };
    }
}