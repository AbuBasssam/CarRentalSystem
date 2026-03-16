using Application.Models;
using Domain.AppMetaData;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class GetCarImagesHandler : IRequestHandler<GetCarImagesQuery, Response<List<CarImageMetadataDto>>>
{
    #region Field(s)

    private readonly ICarImageRepository _imageRepository;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public GetCarImagesHandler(ICarImageRepository imageRepository, ResponseHandler responseHandler)
    {
        _imageRepository = imageRepository;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<List<CarImageMetadataDto>>> Handle(
    GetCarImagesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var images = await _imageRepository
                .GetTableNoTracking()
                .Where(img => img.CarId == request.CarId
                           && img.Car.IsActive
                           && img.Car.CurrentBranch.IsActive
                           && !img.IsDeleted)
                .OrderBy(img => img.Id)
                .Select(img => new CarImageMetadataDto(
                    img.Id,
                    $"{Router.PublicCarRouter.BASE}/{img.CarId}/images/{img.Id}",
                    img.IsPrimary
                ))
                .ToListAsync(cancellationToken);

            // If the list is empty, it means the car doesn't exist, is inactive, or has no images.
            if (images == null || !images.Any())
                return _responseHandler.NotFound<List<CarImageMetadataDto>>();

            return _responseHandler.Success(images);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching images for car {CarId}", request.CarId);
            return _responseHandler.InternalServerError<List<CarImageMetadataDto>>();
        }
    }


    #endregion
}