using Interfaces;

namespace Domain.Entities;
public class Branch : IEntity<int>
{
    public int Id { get; set; }
    public string NameEN { get; set; } = null!;
    public string NameAR { get; set; } = null!;
    public string CityEN { get; set; } = null!;
    public string CityAR { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Car> Cars { get; set; } = null!;

}
