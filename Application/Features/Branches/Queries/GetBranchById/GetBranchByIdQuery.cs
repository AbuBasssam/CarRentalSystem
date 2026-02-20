using Application.Models;
using MediatR;

namespace Application.Features.Branches;
public class GetBranchByIdQuery : IRequest<Response<BranchDetailsDTO>>
{
    public int Id { get; set; }
    public GetBranchByIdQuery(int id) => Id = id;
}
