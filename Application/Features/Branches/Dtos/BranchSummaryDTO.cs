using AutoMapper;
using Domain.Entities;

namespace Application.Features.Branches;

public class BranchSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public bool IsActive { get; set; }

    private class Mapping : Profile
    {
        public Mapping() => CreateMap<Branch, BranchSummaryDTO>();
    }
}
