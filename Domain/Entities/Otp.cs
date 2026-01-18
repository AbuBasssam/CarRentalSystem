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

    // NEW: Track failed attempts
    public int AttemptsCount { get; private set; }

    // NEW: Track last attempt time
    public DateTime? LastAttemptAt { get; private set; }


    private byte MaxAttempts = 5;
    public bool IsUsed { get; private set; }


    public int UserId { get; private set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
    protected Otp() { }

    public Otp(string code, enOtpType type, int userId, TimeSpan validFor)
    {
        if (validFor <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(validFor), "OTP validity duration must be positive");

        DateTime creationTime = DateTime.UtcNow;
        TimeSpan sendTimeBuffer = TimeSpan.FromMinutes(1);
        var compensatedValidity = validFor + sendTimeBuffer;

        Code = code;
        Type = type;
        UserId = userId;
        CreationTime = creationTime;
        ExpirationTime = creationTime.Add(compensatedValidity);
        AttemptsCount = 0;
        LastAttemptAt = null;
    }
    public bool IsExpired() => DateTime.UtcNow >= ExpirationTime;
    public void UpdateLastAttempt() => LastAttemptAt = DateTime.UtcNow;

    /// <summary>
    /// Force OTP to expire immediately
    /// </summary>
    public void ForceExpire()
    {
        if (IsExpired())
            return;

        ExpirationTime = DateTime.UtcNow;
        IsUsed = true;
    }


    /// <summary>
    /// Increment failed verification attempts
    /// </summary>
    public void IncrementAttempts()
    {
        AttemptsCount++;
        LastAttemptAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if OTP has exceeded maximum attempts
    /// </summary>
    public bool HasExceededMaxAttempts() => AttemptsCount >= MaxAttempts;

    /// <summary>
    /// Check if OTP is valid for verification
    /// </summary>
    public bool IsValidForVerification() => !IsExpired() && !IsUsed && !HasExceededMaxAttempts();





    private TimeSpan _GetCooldownPeriod()
    {
        return Type switch
        {
            enOtpType.ConfirmEmail => HasExceededMaxAttempts() ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(2),
            enOtpType.ResetPassword => HasExceededMaxAttempts() ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(2)
        };
    }
    public (bool canResend, TimeSpan? remaining) CanResend()
    {
        var now = DateTime.UtcNow;
        var cooldownPeriod = _GetCooldownPeriod();


        var cooldownEndsAt = CreationTime.Add(cooldownPeriod);

        if (now < cooldownEndsAt)
        {
            return (false, cooldownEndsAt - now);
        }

        return (true, null);
    }



}


