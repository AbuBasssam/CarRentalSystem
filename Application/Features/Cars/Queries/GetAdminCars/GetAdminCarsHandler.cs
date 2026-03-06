using Application.Extensions;
using Application.Models;
using Domain.Entities;
using Interfaces;
using MediatR;
using Serilog;
using System.Linq.Expressions;

namespace Application.Features.Cars;

public class GetAdminCarsHandler : IRequestHandler<GetAdminCarsQuery, Response<CursorPaginatedResult<AdminCarSummaryDto>>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly IRequestContext _requestContext;

    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public GetAdminCarsHandler(ICarRepository carRepository, ResponseHandler responseHandler, IRequestContext requestContext)
    {
        _carRepository = carRepository;
        _responseHandler = responseHandler;
        _requestContext = requestContext;
    }

    #endregion

    #region Handler

    public async Task<Response<CursorPaginatedResult<AdminCarSummaryDto>>> Handle(GetAdminCarsQuery request, CancellationToken cancellationToken)
    {

        try
        {
            var query = _carRepository.GetTableNoTracking();

            if (request.Cursor.HasValue)
                query = query.Where(c => c.Id > request.Cursor.Value);

            var result = await query
                            .ApplyFilterAndSort(request)
                            .ToCursorPaginatedAsync(
                                request.PageSize,
                                AdminCarSummaryDtoBuilder(),
                                dto => dto.Id,
                                cancellationToken
                            );

            Log.Information("Fetched {Count} cars for admin list for admin Id {Id}.", result.Count, _requestContext.UserId == null ? "unkown" : _requestContext.UserId);

            return _responseHandler.CursorPaginated(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching admin cars list");
            return _responseHandler.InternalServerError<CursorPaginatedResult<AdminCarSummaryDto>>();
        }
    }




    #endregion
    #region Helpers
    private static Expression<Func<Car, AdminCarSummaryDto>> AdminCarSummaryDtoBuilder()
    {
        return c => new AdminCarSummaryDto
        {
            Id = c.Id,
            PlateNumberEN = c.PlateNumberEN,
            PlateNumberAR = c.PlateNumberAR,
            Brand = c.Brand,
            Model = c.Model,
            Year = c.Year,
            CategoryName = c.Category.NameEN,
            CurrentBranchName = c.CurrentBranch.NameEN,
            IsActive = c.IsActive,
            FleetConditionStatus = c.FleetConditionStatus.ToDisplayName()
        };
    }


    #endregion


}
