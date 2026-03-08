using Application.Models;
using MediatR;

namespace Application.Features.Cars;

public record GetCarImagesQuery(int CarId) : IRequest<Response<List<CarImageMetadataDto>>>;
