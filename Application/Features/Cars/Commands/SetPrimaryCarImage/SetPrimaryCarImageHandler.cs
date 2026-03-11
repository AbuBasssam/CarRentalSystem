using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class SetPrimaryCarImageHandler : IRequestHandler<SetPrimaryCarImageCommand, Response<bool>>
{
    #region Field(s)

    private readonly ICarImageRepository _imageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public SetPrimaryCarImageHandler(
        ICarImageRepository imageRepository,
        IUnitOfWork unitOfWork,
        ResponseHandler responseHandler)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<bool>> Handle(
        SetPrimaryCarImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Load target image
            var targetImage = await _imageRepository
                .GetTableAsTracking()
                .FirstOrDefaultAsync(img =>
                    img.Id == request.ImageId
                    && img.CarId == request.CarId
                    && !img.IsDeleted,
                    cancellationToken);

            if (targetImage is null)
                return _responseHandler.NotFound<bool>("Image not found.");

            if (targetImage.IsPrimary)
                return _responseHandler.Success(true);

            // Demote the current primary
            var currentPrimary = await _imageRepository
                .GetTableAsTracking()
                .FirstOrDefaultAsync(img =>
                    img.CarId == request.CarId
                    && img.IsPrimary
                    && !img.IsDeleted,
                    cancellationToken);

            if (currentPrimary is not null)
                currentPrimary.IsPrimary = false;

            targetImage.IsPrimary = true;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting primary image {ImageId} for car {CarId}",
                request.ImageId, request.CarId);
            return _responseHandler.InternalServerError<bool>();
        }
    }

    #endregion
}