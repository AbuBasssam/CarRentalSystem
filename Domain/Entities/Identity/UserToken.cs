using Domain.Enums;
using Interfaces;

namespace Domain.Entities;


public class UserToken : IEntity<int>
{
    public int Id { get; set; }
    public enTokenType Type { get; set; }
    public int? UserId { get; set; }
    public virtual User? User { get; set; }
    public string? RefreshToken { get; set; }
    public string? JwtId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired => IsRevoked || DateTime.UtcNow >= ExpiryDate;

    private UserToken() { }
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
            throw new Exception("Token validity duration must be positive");
        //throw new DomainException("Token validity duration must be positive");

        UserId = userId;
        Type = type;
        RefreshToken = refreshTokenHash;
        JwtId = jwtId;

        CreatedAt = DateTime.UtcNow;
        ExpiryDate = CreatedAt?.Add(validFor);
    }


    /// <summary>
    /// Mark token as used (Refresh scenario)
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsExpired)
            throw new Exception("Cannot use expired or revoked token");
        // throw new DomainException("Cannot use expired or revoked token");

        if (IsUsed)
            throw new Exception("Token already used");
        //throw new DomainException("Token already used");

        IsUsed = true;
    }

    /// <summary>
    /// Revoke token explicitly (logout, security breach)
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
    }

    /// <summary>
    /// Force token to expire immediately
    /// </summary>
    public void ForceExpire()
    {
        if (IsExpired)
            return;

        ExpiryDate = DateTime.UtcNow;
    }


}
