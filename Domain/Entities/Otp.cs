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

    public bool IsExpired => DateTime.UtcNow >= ExpirationTime;
    public bool IsUsed { get; private set; }


    public int UserID { get; private set; }

    // Navigation property
    public virtual User User { get; set; } = null!;
    private Otp() { }

    public Otp(string code, enOtpType type, int userId, TimeSpan validFor)
    {
        if (validFor <= TimeSpan.Zero)
            throw new Exception("OTP validity duration must be positive");
        // throw new DomainException("OTP validity duration must be positive");
        Code = code;
        Type = type;
        UserID = userId;
        CreationTime = DateTime.UtcNow;
        ExpirationTime = CreationTime.Add(validFor);
    }

    /// <summary>
    /// Force OTP to expire immediately
    /// </summary>
    public void ForceExpire()
    {
        if (IsExpired)
            return;

        ExpirationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark otp as used 
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsExpired)
            throw new Exception("Cannot use expired OTP");
        //throw new DomainException("Cannot use expired OTP");

        if (IsUsed)
            throw new Exception("OTP already used");
        //throw new DomainException("OTP already used");

        IsUsed = true;
    }


}


