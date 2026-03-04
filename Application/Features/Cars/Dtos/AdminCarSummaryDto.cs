namespace Application.Features.Cars;

/// <summary>
/// Admin list view — includes sensitive fields,
/// shows all statuses.
/// </summary>
public record AdminCarSummaryDto
{
    public int Id { get; init; }
    public string PlateNumberEN { get; init; } = null!;
    public string PlateNumberAR { get; init; } = null!;
    public string Brand { get; init; } = null!;
    public string Model { get; init; } = null!;
    public int Year { get; init; }
    public string CategoryName { get; init; } = null!;
    public string CurrentBranchName { get; init; } = null!;
    public bool IsActive { get; init; }
    public string FleetConditionStatus { get; init; } = null!;
}
