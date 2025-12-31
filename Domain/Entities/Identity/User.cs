using Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class User : IdentityUser<int>, IEntity<int>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EmailConfirmedAt { get; set; }

        public virtual ICollection<UserToken> RefreshTokens { get; } = null!;
        public virtual ICollection<UserRole> UserRoles { get; set; } = null!;
        public virtual ICollection<Otp> Otps { get; set; } = null!;

    }
}
