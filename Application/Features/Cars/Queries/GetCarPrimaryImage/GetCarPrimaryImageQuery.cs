using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public record GetCarPrimaryImageQuery(int CarId) : IRequest<Response<CarImageFileDto>>;
