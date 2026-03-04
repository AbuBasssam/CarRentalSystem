namespace Application.Features.Cars;

/// <summary>
/// Public list view.
/// PlateNumber and VIN are never included — Late Assignment Strategy.
/// </summary>
public record CustomerCarSummaryDto
{
    public int Id { get; init; }
    public string Brand { get; init; } = null!;
    public string Model { get; init; } = null!;
    public int Year { get; init; }
    public string FuelType { get; init; } = null!;
    public string TransmissionType { get; init; } = null!;
    public byte NumberOfSeats { get; set; }
    public byte NumberOfBags { get; set; }

    public string CategoryName { get; init; } = null!;
    public decimal DailyRate { get; init; }

    /// <summary>Null if car has no images uploaded yet.</summary>
    public string? PrimaryImageUrl { get; init; }

    public string BranchName { get; init; } = null!;
}
