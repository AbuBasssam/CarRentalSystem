using AutoMapper;
using Domain.Entities;

namespace Application.Features.Branches;

public class BranchDetailsDTO
{
    public int Id { get; set; }
    public string NameEN { get; set; } = null!;
    public string NameAR { get; set; } = null!;
    public string CityEN { get; set; } = null!;
    public string CityAR { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }

    private class Mapping : Profile
    {
        public Mapping() => CreateMap<Branch, BranchDetailsDTO>();
    }
}