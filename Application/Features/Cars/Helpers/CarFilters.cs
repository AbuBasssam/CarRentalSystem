namespace Application.Features.Cars;
public class CarFilters
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? CategoryId { get; set; }
    public int? BranchId { get; set; }
    public int? TransmissionType { get; set; }
    public int? FuelType { get; set; }
    public double? MinDailyRate { get; set; }
    public double? MaxDailyRate { get; set; }

}
