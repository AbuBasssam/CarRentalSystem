using Application.Abstracts;
using Application.Models;
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

    /// <summary>
    /// Projects and paginates a query using cursor-based logic.
    /// Fetches PageSize + 1 items to determine if a next page exists without a separate COUNT query.
    /// </summary>
    /// <typeparam name="TEntity">The database entity type.</typeparam>
    /// <typeparam name="TDto">The result DTO type.</typeparam>
    /// <param name="query">The IQueryable to paginate.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cursorSelector">Expression to get the DTO values.</param>
    /// <param name="idSelector">Expression to get the ID/Cursor value from the DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A CursorPaginatedResult containing items and the next cursor.</returns>
    public static async Task<CursorPaginatedResult<TDto>> ToCursorPaginatedAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        int pageSize,
        Expression<Func<TEntity, TDto>> cursorSelector,
        Func<TDto, int?> idSelector,
        CancellationToken cancellationToken = default) where TDto : class
    {
        // Fetch PageSize + 1 to check for the existence of the next page
        var items = await query
            .Take(pageSize + 1)
            .Select(cursorSelector)
            .ToListAsync(cancellationToken);

        int? nextCursor = null;

        if (items.Count > pageSize)
        {
            // The cursor for the next request is the ID of the last item in the CURRENT page
            // We use the cursorSelector to get the Id from the TDto
            nextCursor = idSelector(items[pageSize - 1]);

            // Remove the extra (N+1) item from the final list
            items.RemoveAt(items.Count - 1);
        }

        return new CursorPaginatedResult<TDto>
        {
            Items = items,
            NextCursor = nextCursor
        };
    }
}