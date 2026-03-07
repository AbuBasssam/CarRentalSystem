using Application.Extensions;
using Application.Models;
using Domain.AppMetaData;
using Domain.HelperClasses;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class GetPublicCarByIdHandler : IRequestHandler<GetPublicCarByIdQuery, Response<CustomerCarDetailsDto>>
{
    #region Field(s)
    private readonly IRequestContext _requestContext;
    private readonly ICarRepository _carRepository;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public GetPublicCarByIdHandler(IRequestContext requestContext, ICarRepository carRepository, ResponseHandler responseHandler)
    {
        _requestContext = requestContext;

        _carRepository = carRepository;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<CustomerCarDetailsDto>> Handle(
        GetPublicCarByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _carRepository
                .GetTableNoTracking()
                .Where(c => c.Id == request.Id && c.IsActive && c.CurrentBranch.IsActive)
                .Select(c => new CustomerCarDetailsDto
                {
                    Id = c.Id,
                    Brand = c.Brand,
                    Model = c.Model,
                    Year = c.Year,
                    FuelType = FuelType.FromId(c.FuelType)!.NameEN,
                    TransmissionType = c.TransmissionType.ToDisplayName(),
                    CategoryName = c.Category.NameEN,
                    CategoryDescription = c.Category.Description,
                    DailyRate = c.CustomDailyRate.HasValue ? c.CustomDailyRate.Value : c.Category.BaseDailyRate,
                    WeeklyRate = c.CustomWeeklyRate.HasValue ? c.CustomWeeklyRate.Value : c.Category.BaseWeeklyRate,
                    MonthlyRate = c.CustomMonthlyRate.HasValue ? c.CustomMonthlyRate.Value : c.Category.BaseMonthlyRate,

                    // Policy hierarchy resolution — single query via LINQ projection
                    BufferHours = c.PolicyOverrideId != null
                        ? c.PolicyOverride!.BufferHours
                        : c.Category.Policy.BufferHours,

                    AllowDifferentDropOff = c.PolicyOverrideId != null
                        ? c.PolicyOverride!.AllowDifferentDropOff
                        : c.Category.Policy.AllowDifferentDropOff,

                    BranchName = c.CurrentBranch.NameEN,
                    Images = c.Images
                        .Where(i => !i.IsDeleted)
                        .Select(i => $"{Router.PublicCarRouter.BASE}/{c.Id}/images/{i.Id}")
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (dto is null)
            {
                Log.Warning(
                  "GetPublicCarById: Car not found. CarId={CarId}, Ip Address: {IpAddress}",
                  request.Id,
                  _requestContext.ClientIP
                );
                return _responseHandler.NotFound<CustomerCarDetailsDto>("Car not found.");
            }
            Log.Information(
              "GetPublicCarById: fetching car with Id:{CarId} Success for Ip Address: {IpAddress}",
              dto.Id,
              _requestContext.ClientIP
            );
            return _responseHandler.Success(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetPublicCarById: fetching admin car with Id:{CarId} failed for Ip Address: {IpAddress}", request.Id, _requestContext.ClientIP);
            return _responseHandler.InternalServerError<CustomerCarDetailsDto>();
        }
    }

    #endregion
}
