using Application.Extensions;
using Application.Models;
using Domain.AppMetaData;
using Domain.HelperClasses;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars.Queries;

public class GetAdminCarByIdHandler : IRequestHandler<GetAdminCarByIdQuery, Response<AdminCarDetailsDto>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;

    private readonly ResponseHandler _responseHandler;
    private readonly IRequestContext _requestContext;

    #endregion

    #region Constructor(s)

    public GetAdminCarByIdHandler(ICarRepository carRepository, ResponseHandler responseHandler, IRequestContext requestContext)
    {
        _carRepository = carRepository;
        _responseHandler = responseHandler;
        _requestContext = requestContext;
    }

    #endregion

    #region Handler

    public async Task<Response<AdminCarDetailsDto>> Handle(
        GetAdminCarByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _carRepository
                .GetTableNoTracking()
                .Where(c => c.Id == request.Id)
                .Select(c => new AdminCarDetailsDto
                {
                    Id = c.Id,
                    PlateNumberEN = c.PlateNumberEN,
                    PlateNumberAR = c.PlateNumberAR,
                    VIN = c.VIN,
                    Brand = c.Brand,
                    Model = c.Model,
                    Year = c.Year,
                    FuelType = FuelType.FromId(c.FuelType)!.NameEN,
                    TransmissionType = c.TransmissionType.ToDisplayName(),
                    Category = new AdminCarDetailsDto.CategoryRef(c.CategoryId, c.Category.NameEN),
                    CurrentBranch = new AdminCarDetailsDto.BranchRef(c.CurrentBranchId, c.CurrentBranch.NameEN),
                    IsActive = c.IsActive,
                    FleetConditionStatus = c.FleetConditionStatus.ToDisplayName(),
                    DailyRate = c.Category.BaseDailyRate,
                    WeeklyRate = c.Category.BaseWeeklyRate,
                    MonthlyRate = c.Category.BaseMonthlyRate,
                    CreatedAt = c.CreatedAt,
                    // Admin image URLs use admin serving endpoint (bypasses public gate)
                    Images = c.Images
                        .Where(i => !i.IsDeleted)
                        .Select(i => $"{Router.PublicCarRouter.BASE}/{c.Id}/images/{i.Id}")
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (dto is null)
            {
                Log.Warning(
                   "GetAdminCarById: Car not found. CarId={CarId}, AdminUserId={AdminUserId}",
                   request.Id,
                   _requestContext.UserId
                );
                return _responseHandler.NotFound<AdminCarDetailsDto>("Car not found.");
            }
            Log.Information(
               "GetAdminCarById: fetching admin car with Id:{CarId} Success for AdminId= {AdminUserId}",
               dto.Id,

               _requestContext.UserId
           );
            return _responseHandler.Success(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetAdminCarById: fetching admin car with Id:{CarId} failed for AdminId= {AdminUserId}", request.Id, _requestContext.UserId);
            return _responseHandler.InternalServerError<AdminCarDetailsDto>();
        }
    }

    #endregion
}