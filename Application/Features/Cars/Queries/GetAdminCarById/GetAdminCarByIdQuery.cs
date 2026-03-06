using Application.Models;
using MediatR;

namespace Application.Features.Cars.Queries;
public record GetAdminCarByIdQuery(int Id) : IRequest<Response<AdminCarDetailsDto>>;
