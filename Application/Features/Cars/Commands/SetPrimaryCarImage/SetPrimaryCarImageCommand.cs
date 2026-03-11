using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public record SetPrimaryCarImageCommand(int CarId, int ImageId) : IRequest<Response<bool>>;
