using Application.Models;
using Domain.Entities;
using Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class UploadCarImagesHandler : IRequestHandler<UploadCarImagesCommand, Response<List<int>>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly ICarImageRepository _imageRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public UploadCarImagesHandler(
        ICarRepository carRepository,
        ICarImageRepository imageRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ResponseHandler responseHandler)
    {
        _carRepository = carRepository;
        _imageRepository = imageRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<List<int>>> Handle(
        UploadCarImagesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var car = await _carRepository
                .GetTableAsTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return _responseHandler.NotFound<List<int>>("Car not found.");

            var imagesToAdd = await PrepareCarImagesAsync(request.CarId, request.Files, cancellationToken);

            var addingResult = car.AddImages(imagesToAdd);

            if (!addingResult.IsSuccess)
                return _responseHandler.BadRequest<List<int>>(addingResult.reason);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var savedIds = imagesToAdd.Select(i => i.Id).ToList();

            return _responseHandler.Created(savedIds);

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error uploading images for car {CarId}", request.CarId);
            return _responseHandler.InternalServerError<List<int>>();
        }
    }

    #endregion

    #region Private Methods
    private async Task<List<CarImage>> PrepareCarImagesAsync(int carId, IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var images = new List<CarImage>();

        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            var fileName = await _fileStorage.SaveCarImageAsync(carId, stream, cancellationToken);

            var image = new CarImage
            {
                CarId = carId,
                FileName = fileName
            };

            images.Add(image);
        }

        return images;
    }
    #endregion

}