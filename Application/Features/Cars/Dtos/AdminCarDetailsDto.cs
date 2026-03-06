namespace Application.Features.Cars;

/// <summary>Admin detail view — full fields, nested IDs wrapped in objects.</summary>
public record AdminCarDetailsDto
{
    public int Id { get; init; }
    public string PlateNumberEN { get; init; } = null!;
    public string PlateNumberAR { get; init; } = null!;
    public string VIN { get; init; } = null!;
    public string Brand { get; init; } = null!;
    public string Model { get; init; } = null!;
    public int Year { get; init; }
    public string FuelType { get; init; } = null!;
    public string TransmissionType { get; init; } = null!;
    public CategoryRef Category { get; init; } = null!;
    public BranchRef CurrentBranch { get; init; } = null!;
    public bool IsActive { get; init; }
    public string FleetConditionStatus { get; init; } = null!;
    public decimal DailyRate { get; init; }
    public decimal WeeklyRate { get; init; }
    public decimal MonthlyRate { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Admin image URLs via admin serving endpoint — bypasses public gate.</summary>
    public List<string> Images { get; init; } = new();

    // ─── Nested reference types ───────────────────────────────────────────────
    // Raw IDs are never exposed flat — wrapped to prevent enumeration attacks.

    public record CategoryRef(int Id, string Name);
    public record BranchRef(int Id, string Name);
}
