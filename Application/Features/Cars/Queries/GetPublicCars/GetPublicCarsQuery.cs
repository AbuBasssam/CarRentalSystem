using Application.Abstracts;
using Application.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Cars;

public class GetPublicCarsQuery : CursorPaginationQuery, IRequest<Response<CursorPaginatedResult<CustomerCarSummaryDto>>>, IFilterable<Car>
{
    public CarFilters Filters { get; set; } = null!;


    public IQueryable<Car> ApplyFilters(IQueryable<Car> query)
    {
        if (!string.IsNullOrWhiteSpace(Filters.Brand))
            query = query.Where(c => c.Brand.Contains(Filters.Brand));

        if (!string.IsNullOrWhiteSpace(Filters.Model))
            query = query.Where(c => c.Model.Contains(Filters.Model));

        if (Filters.CategoryId.HasValue)
            query = query.Where(c => c.CategoryId == Filters.CategoryId.Value);

        if (Filters.BranchId.HasValue)
            query = query.Where(c => c.CurrentBranchId == Filters.BranchId.Value);

        if (Filters.TransmissionType.HasValue)
            query = query.Where(c => c.TransmissionType == Filters.TransmissionType.Value);

        if (Filters.FuelType != null)
            query = query.Where(c => c.FuelType == (byte)Filters.FuelType.Id);

        if (Filters.MinDailyRate.HasValue)
        {
            query = query.Where(_minDailyRateFilterHandler((decimal)Filters.MinDailyRate.Value));
        }

        if (Filters.MaxDailyRate.HasValue)
        {
            query = query.Where(_maxDailyRateFilterHandler((decimal)Filters.MaxDailyRate.Value));
        }
        query.Where(c => c.IsActive);


        return query;
    }
    public IQueryable<Car> ApplySort(IQueryable<Car> query) => query.OrderBy(b => b.Id);

    #region Helpers

    private Expression<Func<Car, bool>> _minDailyRateFilterHandler(decimal min)
    {
        return
                        c => c.CustomDailyRate.HasValue ?
                        c.CustomDailyRate >= min || c.Category.BaseDailyRate >= min :
                        c.Category.BaseDailyRate >= min;
    }

    private Expression<Func<Car, bool>> _maxDailyRateFilterHandler(decimal max)
    {
        return
                        c => c.CustomDailyRate.HasValue ?
                        c.CustomDailyRate <= max || c.Category.BaseDailyRate <= max :
                        c.Category.BaseDailyRate <= max;
    }

    #endregion


}
