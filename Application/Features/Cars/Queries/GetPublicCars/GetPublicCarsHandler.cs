using Application.Extensions;
using Application.Models;
using Domain.AppMetaData;
using Domain.Entities;
using Domain.HelperClasses;
using FluentValidation;
using Interfaces;
using MediatR;
using Serilog;
using System.Linq.Expressions;

namespace Application.Features.Cars;

public class GetPublicCarsHandler : IRequestHandler<GetPublicCarsQuery, Response<CursorPaginatedResult<CustomerCarSummaryDto>>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly IRequestContext _requestContext;

    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public GetPublicCarsHandler(ICarRepository carRepository, ResponseHandler responseHandler, IRequestContext requestContext)
    {
        _carRepository = carRepository;
        _responseHandler = responseHandler;
        _requestContext = requestContext;
    }

    #endregion

    #region Handler

    public async Task<Response<CursorPaginatedResult<CustomerCarSummaryDto>>> Handle(GetPublicCarsQuery request, CancellationToken cancellationToken)
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
                                CustomerCarSummaryDtoBuilder(_requestContext.Language),
                                dto => dto.Id,
                                cancellationToken
                            );

            Log.Information("GetPublicCarsQuery: Fetched {Count} cars for admin list for admin Id {Id}.", result.Count, _requestContext.UserId == null ? "unkown" : _requestContext.UserId);

            return _responseHandler.CursorPaginated(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetPublicCarsQuery: Error fetching admin cars list");
            return _responseHandler.InternalServerError<CursorPaginatedResult<CustomerCarSummaryDto>>();
        }
    }




    #endregion
    #region Helpers
    private static Expression<Func<Car, CustomerCarSummaryDto>> CustomerCarSummaryDtoBuilder(string lang)
    {
        return c => new CustomerCarSummaryDto
        {
            Id = c.Id,
            Brand = c.Brand,
            Model = c.Model,
            Year = c.Year,
            FuelType = FuelType.FromId(c.FuelType)!.GetLocalizedName(lang),
            TransmissionType = c.TransmissionType.ToLocalizeDisplayName(lang),
            CategoryName = lang.ToLower() == "ar" ? c.Category.NameAR : c.Category.NameEN,
            DailyRate = c.Category.BaseDailyRate,
            PrimaryImageUrl = c.Images
                        .Where(i => i.IsPrimary && !i.IsDeleted)
                        .Select(i => $"{Router.PublicCarRouter.BASE}/{c.Id}/primary-image")
                        .FirstOrDefault(),
            BranchName = c.CurrentBranch.NameEN
        };
    }


    #endregion


}
