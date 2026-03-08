using Application.Models;
using Domain.Entities;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class GetCarImageHandler : IRequestHandler<GetCarImageQuery, Response<CarImageFileDto>>
{
    #region Field(s)

    private readonly ICarImageRepository _imageRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public GetCarImageHandler(
        ICarImageRepository imageRepository,
        IFileStorageService fileStorage,
        ResponseHandler responseHandler)
    {
        _imageRepository = imageRepository;
        _fileStorage = fileStorage;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<CarImageFileDto>> Handle(
        GetCarImageQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<CarImage> query = _imageRepository
                .GetTableNoTracking()
                .Where(img => img.Id == request.ImageId
                           && img.CarId == request.CarId
                           && !img.IsDeleted);

            // Public gate: car must be active and branch must be active
            if (!request.IsAdminRequest)
                query = query.Where(img => img.Car.IsActive && img.Car.CurrentBranch.IsActive);

            var fileName = await query
                .Select(img => img.FileName)
                .FirstOrDefaultAsync(cancellationToken);

            if (fileName is null)
                return _responseHandler.NotFound<CarImageFileDto>();

            var (content, contentType) = await _fileStorage.GetCarImageAsync(fileName, cancellationToken);

            return _responseHandler.Success(new CarImageFileDto
            {
                Content = content,
                ContentType = contentType,
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error serving image {ImageId} for car {CarId}", request.ImageId, request.CarId);
            return _responseHandler.InternalServerError<CarImageFileDto>();
        }
    }

    #endregion
}
