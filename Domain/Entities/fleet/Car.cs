using Domain.Enums;
using Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
namespace Domain.Entities;

public class Car : IEntity<int>
{
    public int Id { get; set; }
    public int KmMileage { get; set; } = 0;

    public string PlateNumberEN { get; set; } = null!;
    public string PlateNumberAR { get; set; } = null!;
    public string VIN { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public byte NumberOfSeats { get; set; }
    public byte NumberOfBags { get; set; }
    public short EngineCapacity { get; set; }
    public int Year { get; set; }
    public byte FuelType { get; set; }
    public enTransmissionType TransmissionType { get; set; }
    public enFleetConditionStatus FleetConditionStatus { get; set; } = enFleetConditionStatus.Ready;
    public bool IsActive { get; set; } = true; //OperationalStatus

    // Foreign Keys
    public int CurrentBranchId { get; set; }
    public int CategoryId { get; set; }
    public int? PolicyOverrideId { get; set; }


    // Navigation Properties
    public virtual Branch CurrentBranch { get; set; } = null!;
    public virtual CarCategory Category { get; set; } = null!;
    public virtual RentalPolicy? PolicyOverride { get; set; }

    public virtual ICollection<CarImage> Images { get; set; } = null!;
    public virtual ICollection<CarBranchHistory> BranchHistories { get; set; } = null!;

    [NotMapped]
    public HelperClasses.FuelType FuelTypeObject
    {
        get => HelperClasses.FuelType.FromId(FuelType)
            ?? throw new InvalidOperationException($"Invalid FuelType Id: {FuelType}");
        set => FuelType = (byte)value.Id;
    }
}
