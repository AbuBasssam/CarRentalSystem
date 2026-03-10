using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class DeleteCarImageHandler : IRequestHandler<DeleteCarImageCommand, Response<bool>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public DeleteCarImageHandler(ICarRepository carRepository, IUnitOfWork unitOfWork, ResponseHandler responseHandler)
    {
        _carRepository = carRepository;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<bool>> Handle(DeleteCarImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var car = await _carRepository.GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return _responseHandler.NotFound<bool>("Car not found.");

            var removingResult = car.RemoveImage(request.ImageId);

            if (!removingResult.IsSuccess)
                return _responseHandler.BadRequest<bool>(removingResult.reason);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Deleted<bool>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting image {ImageId} for car {CarId}", request.ImageId, request.CarId);
            return _responseHandler.InternalServerError<bool>();
        }
    }

    #endregion
}