using Application.Abstracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Extensions;

public static class QueryableExtensions
{
    // -----------------------------------------------------------------------
    // 1. Apply filters + sort in one call
    // -----------------------------------------------------------------------

    /// <summary>
    /// Pipes the query through IFilterable.ApplyFilters then IFilterable.ApplySort.
    /// Call this before pagination.
    /// </summary>
    public static IQueryable<T> ApplyFilterAndSort<T>(this IQueryable<T> query, IFilterable<T> filter)
    {
        query = filter.ApplyFilters(query);
        query = filter.ApplySort(query);
        return query;
    }

    // -----------------------------------------------------------------------
    // 2. Paginate + project to DTO in a single async trip to the database
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns (projected data list, total count) without tracking.
    /// Executes exactly two SQL queries: COUNT and SELECT with OFFSET/FETCH.
    /// </summary>
    public static async Task<(List<TDto> Data, int TotalCount)> ToPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, TDto>> selector,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return (data, totalCount);
    }

    // -----------------------------------------------------------------------
    // 3. Helpers for dynamic OrderBy via Expression (used inside ApplySort)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies ascending or descending order based on a flag.
    /// Keeps ApplySort implementations clean and DRY.
    /// </summary>
    public static IQueryable<T> OrderByDirection<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> keySelector, bool descending)
    {
        return descending
                ? query.OrderByDescending(keySelector)
                : query.OrderBy(keySelector);
    }
}