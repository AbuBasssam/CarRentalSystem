using Interfaces;

namespace Domain.Entities;

public class CarImage : IEntity<int>
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int CarId { get; set; }

    public bool IsPrimary { get; set; }

    // Foreign Key

    // Navigation Property
    public virtual Car Car { get; set; } = null!;
}
