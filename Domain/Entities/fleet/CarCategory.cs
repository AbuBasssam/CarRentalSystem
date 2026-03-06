using Interfaces;

namespace Domain.Entities;

public class CarCategory : IEntity<int>
{
    public int Id { get; set; }
    public string NameEN { get; set; } = null!;
    public string NameAR { get; set; } = null!;
    public string? Description { get; set; } = null!;

    //Specifies whether the booking requires a specific car (Luxury) or any car within the category.
    public bool IsModelSpecific { get; set; }

    // Pricing
    public decimal BaseDailyRate { get; set; }
    public decimal BaseWeeklyRate { get; set; }
    public decimal BaseMonthlyRate { get; set; }
    public int DailyKmLimit { get; set; }

    // Foreign Key
    public int PolicyId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual RentalPolicy Policy { get; set; } = null!;

    public virtual ICollection<Car> Cars { get; set; } = null!;

}
