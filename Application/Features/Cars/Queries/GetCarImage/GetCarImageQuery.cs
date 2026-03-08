using Application.Models;
using MediatR;

namespace Application.Features.Cars;


public record GetCarImageQuery(int CarId, int ImageId, bool IsAdminRequest = false) : IRequest<Response<CarImageFileDto>>;
