using Application.Models;
using MediatR;

namespace Application.Features.Branches;
public class UpdateBranchCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public UpdateBranchDTO Dto { get; set; }

    public UpdateBranchCommand(int id, UpdateBranchDTO dto)
    {
        Id = id;
        Dto = dto;
    }
}
