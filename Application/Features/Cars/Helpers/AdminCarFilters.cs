namespace Application.Features.Cars;

public class AdminCarFilters : CarFilters
{
    // Admin-only filters
    public bool? IsActive { get; set; }
    public int? FleetConditionStatus { get; set; }

}