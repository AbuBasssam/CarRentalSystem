using Application.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.Cars;

public class UploadCarImagesCommand : IRequest<Response<List<int>>>
{
    public int CarId { get; set; }
    public List<IFormFile> Files { get; set; } = new();
    public UploadCarImagesCommand(int carId, List<IFormFile> files) { CarId = carId; Files = files; }
}
