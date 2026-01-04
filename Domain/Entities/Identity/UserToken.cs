using Domain.Enums;
using Interfaces;

namespace Domain.Entities;


public class UserToken : IEntity<int>
{
    public int Id { get; set; }
    public enTokenType Type { get; private set; }
    public int? UserId { get; private set; }
    public virtual User? User { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? JwtId { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsRevoked { get; private set; }

    protected UserToken() { }
    private UserToken(int userId, string? refreshTokenHash, string jwtId, DateTime expiryDate)
    {
        UserId = userId;
        Type = enTokenType.AuthToken;
        RefreshToken = refreshTokenHash;
        JwtId = jwtId;
        IsUsed = true;
        CreatedAt = DateTime.UtcNow;
        ExpiryDate = expiryDate;



    }
    public static UserToken GenerateAuthToken(int userId, string? refreshTokenHash, string jwtId, DateTime expiryDate)
    {
        return new UserToken(userId, refreshTokenHash, jwtId, expiryDate);
    }

    public UserToken(int userId, enTokenType type, string? refreshTokenHash, string jwtId, TimeSpan validFor)
    {
        if (validFor <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(validFor), "Token validity duration must be positive");

        DateTime creationTime = DateTime.UtcNow;
        TimeSpan sendTimeBuffer = TimeSpan.FromSeconds(15);
        var compensatedValidity = validFor + sendTimeBuffer;

        if (type == enTokenType.AuthToken)
        {
            DateTime expiryDate = creationTime.Add(validFor);

            GenerateAuthToken(userId, refreshTokenHash, jwtId, expiryDate);
        }

        UserId = userId;
        Type = type;
        RefreshToken = refreshTokenHash;
        JwtId = jwtId;
        CreatedAt = creationTime;
        ExpiryDate = CreatedAt?.Add(compensatedValidity);
    }


    /// <summary>
    /// Mark token as used (Refresh scenario)
    /// </summary>
    public void MarkAsUsed() => IsUsed = true;

    /// <summary>
    /// Revoke token explicitly (logout, security breach)
    /// </summary>
    public void Revoke() => IsRevoked = true;

    /// <summary>
    /// Force token to expire immediately
    /// </summary>
    public void ForceExpire()
    {
        if (IsExpired())
            return;

        ExpiryDate = DateTime.UtcNow;
    }

    public bool IsValid() => !IsExpired() && !IsRevoked && !IsUsed;
    public bool IsExpired() => DateTime.UtcNow >= ExpiryDate;

}
