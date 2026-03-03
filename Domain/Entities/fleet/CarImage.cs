using Interfaces;

namespace Domain.Entities;

public class CarImage : IEntity<int>
{
    public int Id { get; set; }
    public string FileName { get; set; } = null!;
    public int CarId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Property
    public virtual Car Car { get; set; } = null!;
}