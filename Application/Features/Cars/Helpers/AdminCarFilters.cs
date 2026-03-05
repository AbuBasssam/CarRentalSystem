using Domain.Enums;

namespace Application.Features.Cars;

public class AdminCarFilters : CarFilters
{
    // Admin-only filters
    public bool? IsActive { get; set; }
    public enFleetConditionStatus? FleetConditionStatus { get; set; }

}