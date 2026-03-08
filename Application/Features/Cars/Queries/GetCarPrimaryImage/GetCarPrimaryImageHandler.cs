using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class GetCarPrimaryImageHandler : IRequestHandler<GetCarPrimaryImageQuery, Response<CarImageFileDto>>
{
    #region Field(s)

    private readonly ICarImageRepository _imageRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public GetCarPrimaryImageHandler(
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
        GetCarPrimaryImageQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = await _imageRepository
                .GetTableNoTracking()
                .Where(img =>img.CarId == request.CarId && img.IsPrimary && !img.IsDeleted)
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
            Log.Error(ex, "Error serving primary image for car {CarId}", request.CarId);
            return _responseHandler.InternalServerError<CarImageFileDto>();
        }
    }

    #endregion
}