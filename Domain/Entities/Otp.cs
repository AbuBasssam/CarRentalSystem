using Domain.Enums;
using Interfaces;

namespace Domain.Entities;

public class Otp : IEntity<int>
{
    public int Id { get; set; }

    public string Code { get; private set; } = null!;

    public enOtpType Type { get; private set; }

    public DateTime CreationTime { get; private set; }

    public DateTime ExpirationTime { get; private set; }

    public bool IsUsed { get; private set; }


    public int UserId { get; private set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
    protected Otp() { }

    public Otp(string code, enOtpType type, int userId, TimeSpan validFor)
    {
        if (validFor <= TimeSpan.Zero)
            throw new Exception("OTP validity duration must be positive");

        Code = code;
        Type = type;
        UserId = userId;
        CreationTime = DateTime.UtcNow;
        ExpirationTime = CreationTime.Add(validFor);
    }
    public bool IsExpired() => DateTime.UtcNow >= ExpirationTime;

    private TimeSpan _GetCooldownPeriod()
    {
        return Type switch
        {
            enOtpType.ConfirmEmail => TimeSpan.FromMinutes(2),
            enOtpType.ResetPassword => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(2)
        };
    }

    /// <summary>
    /// Force OTP to expire immediately
    /// </summary>
    public void ForceExpire()
    {
        if (IsExpired())
            return;

        ExpirationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark otp as used 
    /// </summary>
    public void MarkAsUsed() => IsUsed = true;
    public bool CanResend()
    {
        TimeSpan cooldown = _GetCooldownPeriod();
        var timeSince = DateTime.UtcNow - CreationTime;
        return timeSince >= cooldown;
    }

    public TimeSpan? GetRemainingCooldown()
    {
        TimeSpan cooldownPeriod = _GetCooldownPeriod();
        var elapsed = DateTime.UtcNow - CreationTime;
        return elapsed < cooldownPeriod
            ? cooldownPeriod - elapsed
            : null;
    }

}


