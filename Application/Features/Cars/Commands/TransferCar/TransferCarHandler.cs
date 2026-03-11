using Application.Models;
using Domain.Entities;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class TransferCarHandler : IRequestHandler<TransferCarCommand, Response<bool>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly IGenericRepository<Branch, int> _branchRepository;
    private readonly IGenericRepository<CarBranchHistory, int> _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public TransferCarHandler(
        ICarRepository carRepository,
        IGenericRepository<Branch, int> branchRepository,
        IGenericRepository<CarBranchHistory, int> historyRepository,
        IUnitOfWork unitOfWork,
        ResponseHandler responseHandler)
    {
        _carRepository = carRepository;
        _branchRepository = branchRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<bool>> Handle(TransferCarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var car = await _carRepository
                .GetTableAsTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return _responseHandler.NotFound<bool>("Car not found.");

            // Validate target branch
            var isBranchExists = await _branchRepository
            .GetTableNoTracking()
            .AnyAsync(b => b.Id == request.Dto.ToBranchId && b.IsActive, cancellationToken);

            if (!isBranchExists)
                return _responseHandler.BadRequest<bool>("Target branch not found or is inactive.");

            if (car.CurrentBranchId == request.Dto.ToBranchId)
                return _responseHandler.BadRequest<bool>("Car is already at the target branch.");

            // Create history record
            var history = _CreateBranchHistory(
                car.Id,
                car.CurrentBranchId,
                request.Dto.ToBranchId,
                request.Dto.Reason
            );

            await _historyRepository.AddAsync(history);

            // Update car
            car.TransferToBranch(request.Dto.ToBranchId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error transferring car {CarId} to branch {BranchId}",
                request.CarId, request.Dto.ToBranchId);
            return _responseHandler.InternalServerError<bool>();
        }
    }

    #endregion
    #region Helpers

    private CarBranchHistory _CreateBranchHistory(int carId, int fromBranchId, int toBranchId, string? reason = null)
    {
        return new CarBranchHistory
        {
            CarId = carId,
            FromBranchId = fromBranchId,
            ToBranchId = toBranchId,
            MovedAt = DateTime.UtcNow,
            Reason = reason
        };
    }
    #endregion
}