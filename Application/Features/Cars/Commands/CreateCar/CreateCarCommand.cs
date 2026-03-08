using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public class CreateCarCommand : IRequest<Response<int>>
{
    public CreateCarDto Dto { get; set; }
    public CreateCarCommand(CreateCarDto dto) => Dto = dto;
}
