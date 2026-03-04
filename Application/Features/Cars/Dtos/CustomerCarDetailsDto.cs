namespace Application.Features.Cars;

/// <summary>
/// Public details page.
/// bufferHours and allowDifferentDropOff only appear here — relevant at booking evaluation.
/// Resolution: car.PolicyOverrideId != null → override policy, else → category policy.
/// </summary>
public record CustomerCarDetailsDto
{
    public int Id { get; init; }
    public string Brand { get; init; } = null!;
    public string Model { get; init; } = null!;
    public int Year { get; init; }
    public string FuelType { get; init; } = null!;
    public string TransmissionType { get; init; } = null!;
    public string CategoryName { get; init; } = null!;
    public string? CategoryDescription { get; init; }
    public decimal DailyRate { get; init; }
    public decimal WeeklyRate { get; init; }
    public decimal MonthlyRate { get; init; }

    /// <summary>Resolved from PolicyOverride if set, otherwise from Category.Policy.</summary>
    public int BufferHours { get; init; }

    /// <summary>Resolved from PolicyOverride if set, otherwise from Category.Policy.</summary>
    public bool AllowDifferentDropOff { get; init; }

    public string BranchName { get; init; } = null!;
    public List<string> Images { get; init; } = new();
}
