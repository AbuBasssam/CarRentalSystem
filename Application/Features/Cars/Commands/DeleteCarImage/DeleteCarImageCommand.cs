using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public record DeleteCarImageCommand(int CarId, int ImageId) : IRequest<Response<bool>>;
