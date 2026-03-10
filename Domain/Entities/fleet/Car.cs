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
    public decimal? CustomDailyRate { get; set; }
    public decimal? CustomWeeklyRate { get; set; }
    public decimal? CustomMonthlyRate { get; set; }
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
    public DateTime CreatedAt { get; set; }


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

    public (bool IsSuccess, string? reason) AddImages(List<CarImage> imagesToAdd)
    {
        if (imagesToAdd == null || imagesToAdd.Count == 0)
            return (false, "No images to add.");

        var hasPrimary = Images.Any(i => i.IsPrimary && !i.IsDeleted);

        for (int i = 0; i < imagesToAdd.Count; i++)
        {
            var image = imagesToAdd[i];

            //set primary image if not exists
            if (!hasPrimary && i == 0)
            {
                image.IsPrimary = true;
                hasPrimary = true;
            }

            image.CreatedAt = DateTime.UtcNow;
            image.IsDeleted = false;

            Images.Add(image);
        }

        return (true, null);
    }
    public (bool IsSuccess, string? reason) RemoveImage(int imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
        if (image == null)
            return (false, "Image not found.");

        if (image.IsPrimary)
        {
            var nextImage = Images
                .Where(i => i.Id != image.Id && !i.IsDeleted)
                .OrderBy(i => i.Id)
                .FirstOrDefault();

            if (nextImage == null)
                return (false, "Cannot delete the primary image when it is the only image.");

            nextImage.IsPrimary = true;
        }

        image.IsDeleted = true;
        image.IsPrimary = false;
        image.DeletedAt = DateTime.UtcNow;

        return (true, null);

    }
}
