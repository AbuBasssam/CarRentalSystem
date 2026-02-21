using Application.Models;
using MediatR;

namespace Application.Features.Branches;
public class ToggleBranchStatusCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public bool ActiveStatus { get; set; }
    public ToggleBranchStatusCommand(int id, bool activeStatus)
    {
        Id = id;
        ActiveStatus = activeStatus;
    }
}
