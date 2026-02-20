using Application.Models;
using MediatR;

namespace Application.Features.Branches;
public class CreateBranchCommand : IRequest<Response<int>>
{
    public CreateBranchDTO Dto { get; set; }
    public CreateBranchCommand(CreateBranchDTO dto) => Dto = dto;

}
