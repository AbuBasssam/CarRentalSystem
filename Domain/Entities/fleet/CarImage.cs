using Interfaces;

namespace Domain.Entities;



public class CarImage : IEntity<int>
{
    public int Id { get; set; }
    public string FileName { get; private set; } = null!;
    public int CarId { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation Property
    public virtual Car Car { get; private set; } = null!;

    // Required by EF Core
    protected CarImage() { }

    public CarImage(int carId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        CarId = carId;
        FileName = fileName;
        IsPrimary = false;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Promotes this image to primary and demotes the current primary.
    /// </summary>
    public (bool IsSuccess, string? reason) SetAsPrimary(CarImage? currentPrimary)
    {
        if (IsDeleted)
            return (false, "Cannot set a deleted image as primary.");

        if (IsPrimary)
            return (true, null);

        currentPrimary?.Demote();
        IsPrimary = true;

        return (true, null);
    }

    /// <summary>
    /// Soft-deletes this image. Promotes nextPrimary if this image was primary.
    /// </summary>
    public (bool IsSuccess, string? reason) Delete(CarImage? nextPrimary)
    {
        if (IsDeleted)
            return (false, "Image is already deleted.");

        if (IsPrimary)
        {
            if (nextPrimary is null)
                return (false, "Cannot delete the primary image when it is the only image.");

            nextPrimary.IsPrimary = true;
        }

        IsDeleted = true;
        IsPrimary = false;
        DeletedAt = DateTime.UtcNow;

        return (true, null);
    }

    /// <summary>
    /// Strips primary status from this image
    /// </summary>
    private void Demote() => IsPrimary = false;


    /// <summary>
    /// Mark current image as primary image
    /// </summary>
    internal void MarkAsPrimaryOnAdd() => IsPrimary = true; // Used by Car.AddImages to mark the first image as primary during bulk add
}