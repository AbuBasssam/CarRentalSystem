using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public class TransferCarCommand : IRequest<Response<bool>>
{
    public int CarId { get; set; }
    public TransferCarDto Dto { get; set; }
    public TransferCarCommand(int carId, TransferCarDto dto) { CarId = carId; Dto = dto; }
}
