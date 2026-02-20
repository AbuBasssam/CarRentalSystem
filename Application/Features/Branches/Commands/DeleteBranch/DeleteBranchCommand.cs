using Application.Models;
using MediatR;

namespace Application.Features.Branches;
public class DeleteBranchCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public DeleteBranchCommand(int id) => Id = id;
}
