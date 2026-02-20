namespace Application.Abstracts;

/// <summary>
/// Contract for any query that can filter and sort over a given entity.
/// Implement this on every FilterQuery to keep logic co-located with its query.
/// </summary>
/// <typeparam name="TEntity">The domain entity being queried.</typeparam>
public interface IFilterable<TEntity>
{
    /// <summary>Applies WHERE clauses based on the query's filter properties.</summary>
    IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query);

    /// <summary>Applies ORDER BY based on SortBy / SortDir.</summary>
    IQueryable<TEntity> ApplySort(IQueryable<TEntity> query);
}