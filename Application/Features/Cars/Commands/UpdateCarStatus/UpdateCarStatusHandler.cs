using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class UpdateCarStatusHandler : IRequestHandler<UpdateCarStatusCommand, Response<bool>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public UpdateCarStatusHandler(ICarRepository carRepository, IUnitOfWork unitOfWork, ResponseHandler responseHandler)
    {
        _carRepository = carRepository;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<bool>> Handle(UpdateCarStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var car = await _carRepository
                .GetTableAsTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return _responseHandler.NotFound<bool>("Car not found.");

            if (request.Dto.IsActive.HasValue)
            {
                if (request.Dto.IsActive.Value)
                    car.Activate();
                else
                    car.Deactivate();
            }

            if (request.Dto.FleetConditionStatus.HasValue)
                car.UpdateConditionStatus(request.Dto.FleetConditionStatus.Value);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating status for car {CarId}", request.CarId);
            return _responseHandler.InternalServerError<bool>();
        }
    }

    #endregion
}