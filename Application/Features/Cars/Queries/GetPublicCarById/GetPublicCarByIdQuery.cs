using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public record GetPublicCarByIdQuery(int Id) : IRequest<Response<CustomerCarDetailsDto>>;
