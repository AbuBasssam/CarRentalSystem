using Interfaces;

namespace Domain.Entities;

public class CarBranchHistory : IEntity<int>
{
    public int Id { get; set; }
    public DateTime MovedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    // Foreign Keys
    public int CarId { get; set; }
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }

    // Navigation Properties
    public virtual Car Car { get; set; } = null!;
    public virtual Branch FromBranch { get; set; } = null!;
    public virtual Branch ToBranch { get; set; } = null!;
}