using Interfaces;

namespace Domain.Entities;

public class RentalPolicy : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int BufferHours { get; set; }

    public bool AllowDifferentDropOff { get; set; }

    public int MinCancellationLeadTimeHours { get; set; }
    public decimal CancellationPenaltyPercent { get; set; }
    public int NoShowPenaltyDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<CarCategory> Categories { get; set; } = null!;
    public virtual ICollection<Car> CarOverrides { get; set; } = null!;
}