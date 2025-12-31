using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class Role : IdentityRole<int>
{
    public virtual ICollection<UserRole> UserRoles { get; set; } = null!;


}
