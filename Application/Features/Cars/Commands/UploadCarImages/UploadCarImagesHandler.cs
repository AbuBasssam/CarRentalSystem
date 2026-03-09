using Application.Models;
using Domain.Entities;
using Interfaces;
using MediatR;
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
            var carExists = await _carRepository
                .GetTableNoTracking()
                .AnyAsync(c => c.Id == request.CarId, cancellationToken);

            if (!carExists)
                return _responseHandler.NotFound<List<int>>("Car not found.");

            // Check if car already has a primary image
            var hasPrimary = await _imageRepository
                .GetTableNoTracking()
                .AnyAsync(img => img.CarId == request.CarId && img.IsPrimary && !img.IsDeleted,
                    cancellationToken);

            var savedIds = new List<int>();
            var isFirstUpload = !hasPrimary;

            foreach (var (file, index) in request.Files.Select((f, i) => (f, i)))
            {
                using var stream = file.OpenReadStream();
                var fileName = await _fileStorage.SaveCarImageAsync(request.CarId, stream, cancellationToken);

                var image = new CarImage
                {
                    CarId = request.CarId,
                    FileName = fileName,
                    IsPrimary = isFirstUpload && index == 0, // First uploaded image becomes primary if no primary exists
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _imageRepository.AddAsync(image);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                savedIds.Add(image.Id);
            }

            return _responseHandler.Created(savedIds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error uploading images for car {CarId}", request.CarId);
            return _responseHandler.InternalServerError<List<int>>();
        }
    }

    #endregion
}