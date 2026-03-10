using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public class UpdateCarStatusCommand : IRequest<Response<bool>>
{
    public int CarId { get; set; }
    public UpdateCarStatusDto Dto { get; set; }
    public UpdateCarStatusCommand(int carId, UpdateCarStatusDto dto) { CarId = carId; Dto = dto; }
}
