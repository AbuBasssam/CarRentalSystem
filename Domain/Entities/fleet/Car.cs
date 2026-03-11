using Domain.Enums;
using Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Car : IEntity<int>
{
    public int Id { get; set; }
    public int KmMileage { get; private set; }

    public string PlateNumberEN { get; private set; } = null!;
    public string PlateNumberAR { get; private set; } = null!;
    public string VIN { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public decimal? CustomDailyRate { get; private set; }
    public decimal? CustomWeeklyRate { get; private set; }
    public decimal? CustomMonthlyRate { get; private set; }
    public byte NumberOfSeats { get; private set; }
    public byte NumberOfBags { get; private set; }
    public short EngineCapacity { get; private set; }
    public int Year { get; private set; }
    public byte FuelType { get; private set; }
    public enTransmissionType TransmissionType { get; private set; }
    public enFleetConditionStatus FleetConditionStatus { get; private set; } = enFleetConditionStatus.Ready;
    public bool IsActive { get; private set; } = true; //OperationalStatus
    public DateTime CreatedAt { get; private set; }

    // Foreign Keys
    public int CurrentBranchId { get; private set; }
    public int CategoryId { get; private set; }
    public int? PolicyOverrideId { get; private set; }

    // Navigation Properties
    public virtual Branch CurrentBranch { get; private set; } = null!;
    public virtual CarCategory Category { get; private set; } = null!;
    public virtual RentalPolicy? PolicyOverride { get; private set; }
    public virtual ICollection<CarImage> Images { get; private set; } = new List<CarImage>();
    public virtual ICollection<CarBranchHistory> BranchHistories { get; private set; } = new List<CarBranchHistory>();

    [NotMapped]
    public HelperClasses.FuelType FuelTypeObject
    {
        get => HelperClasses.FuelType.FromId(FuelType)
            ?? throw new InvalidOperationException($"Invalid FuelType Id: {FuelType}");
        set => FuelType = (byte)value.Id;
    }

    // Required by EF Core
    protected Car() { }

    public Car(
        string plateNumberEN,
        string plateNumberAR,
        string vin,
        string brand,
        string model,
        int year,
        byte fuelType,
        enTransmissionType transmissionType,
        byte numberOfSeats,
        byte numberOfBags,
        short engineCapacity,
        int currentBranchId,
        int categoryId)
    {
        if (string.IsNullOrWhiteSpace(plateNumberEN)) throw new ArgumentException("English plate number is required.", nameof(plateNumberEN));
        if (string.IsNullOrWhiteSpace(plateNumberAR)) throw new ArgumentException("Arabic plate number is required.", nameof(plateNumberAR));
        if (string.IsNullOrWhiteSpace(vin)) throw new ArgumentException("VIN is required.", nameof(vin));
        if (string.IsNullOrWhiteSpace(brand)) throw new ArgumentException("Brand is required.", nameof(brand));
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Model is required.", nameof(model));
        if (year < 1886 || year > DateTime.UtcNow.Year + 1)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is out of valid range.");

        PlateNumberEN = plateNumberEN;
        PlateNumberAR = plateNumberAR;
        VIN = vin;
        Brand = brand;
        Model = model;
        Year = year;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        NumberOfSeats = numberOfSeats;
        NumberOfBags = numberOfBags;
        EngineCapacity = engineCapacity;
        CurrentBranchId = currentBranchId;
        CategoryId = categoryId;
        KmMileage = 0;
        IsActive = true;
        FleetConditionStatus = enFleetConditionStatus.Ready;
        CreatedAt = DateTime.UtcNow;
    }

    // -------------------------------------------------------------------------
    // Update Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Update core editable details of the car.
    /// </summary>
    public void UpdateDetails(string plateNumberEN, string plateNumberAR, string brand, string model, int year, byte fuelType, enTransmissionType transmissionType,
        byte numberOfSeats, byte numberOfBags, short engineCapacity, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(plateNumberEN)) throw new ArgumentException("English plate number is required.", nameof(plateNumberEN));
        if (string.IsNullOrWhiteSpace(plateNumberAR)) throw new ArgumentException("Arabic plate number is required.", nameof(plateNumberAR));
        if (string.IsNullOrWhiteSpace(brand)) throw new ArgumentException("Brand is required.", nameof(brand));
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Model is required.", nameof(model));
        if (year < 1886 || year > DateTime.UtcNow.Year + 1)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is out of valid range.");

        PlateNumberEN = plateNumberEN;
        PlateNumberAR = plateNumberAR;
        Brand = brand;
        Model = model;
        Year = year;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        NumberOfSeats = numberOfSeats;
        NumberOfBags = numberOfBags;
        EngineCapacity = engineCapacity;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Update or clear the custom pricing rates.
    /// </summary>
    public void UpdateCustomRates(decimal? dailyRate, decimal? weeklyRate, decimal? monthlyRate)
    {
        if (dailyRate.HasValue && dailyRate <= 0) throw new ArgumentOutOfRangeException(nameof(dailyRate), "Daily rate must be positive.");
        if (weeklyRate.HasValue && weeklyRate <= 0) throw new ArgumentOutOfRangeException(nameof(weeklyRate), "Weekly rate must be positive.");
        if (monthlyRate.HasValue && monthlyRate <= 0) throw new ArgumentOutOfRangeException(nameof(monthlyRate), "Monthly rate must be positive.");

        CustomDailyRate = dailyRate;
        CustomWeeklyRate = weeklyRate;
        CustomMonthlyRate = monthlyRate;
    }

    /// <summary>
    /// Record new mileage reading. Cannot go backwards.
    /// </summary>
    public (bool IsSuccess, string? reason) UpdateMileage(int newMileage)
    {
        if (newMileage < KmMileage)
            return (false, $"New mileage ({newMileage}) cannot be less than current mileage ({KmMileage}).");

        KmMileage = newMileage;
        return (true, null);
    }

    /// <summary>
    /// Transfer the car to a different branch.
    /// </summary>
    public (bool IsSuccess, string? reason) TransferToBranch(int newBranchId)
    {
        if (newBranchId == CurrentBranchId)
            return (false, "Car is already assigned to this branch.");

        CurrentBranchId = newBranchId;
        return (true, null);
    }

    /// <summary>
    /// Override or clear the car's rental policy.
    /// </summary>
    public void SetPolicyOverride(int? policyId) => PolicyOverrideId = policyId;

    // -------------------------------------------------------------------------
    // Status Methods
    // -------------------------------------------------------------------------

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
    }

    public void UpdateConditionStatus(enFleetConditionStatus newStatus)
    {
        if (FleetConditionStatus == newStatus)
            return;

        FleetConditionStatus = newStatus;

    }

    // -------------------------------------------------------------------------
    // Image Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Add new images to the car. First image becomes primary if none exists.
    /// </summary>
    public (bool IsSuccess, string? reason) AddImages(List<CarImage> imagesToAdd)
    {
        if (imagesToAdd == null || imagesToAdd.Count == 0)
            return (false, "No images to add.");

        var hasPrimary = Images.Any(i => i.IsPrimary && !i.IsDeleted);

        for (int i = 0; i < imagesToAdd.Count; i++)
        {
            var image = imagesToAdd[i];

            if (!hasPrimary && i == 0)
            {
                image.MarkAsPrimaryOnAdd();
                hasPrimary = true;
            }

            Images.Add(image);
        }

        return (true, null);
    }

    /// <summary>
    /// Soft-delete an image. Promotes next available image to primary if needed.
    /// </summary>
    public (bool IsSuccess, string? reason) RemoveImage(int imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
        if (image is null)
            return (false, "Image not found.");

        var nextPrimary = image.IsPrimary
            ? Images.Where(i => i.Id != imageId && !i.IsDeleted).OrderBy(i => i.Id).FirstOrDefault()
            : null;

        return image.Delete(nextPrimary);
    }
}