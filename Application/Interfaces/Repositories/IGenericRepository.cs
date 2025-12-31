namespace Interfaces;

public interface IGenericRepository<T, TKey> : IScopedService where T : class, IEntity<TKey>
{
    IQueryable<T> GetByIdAsync(TKey id);
    IQueryable<T> GetTableNoTracking();
    IQueryable<T> GetTableAsTracking();
    IQueryable<T> GetPage(int PageNumber = 1);

    Task<bool> IsExistsByIdAsync(TKey id);
    Task<TKey> AddAsync(T entity);
    bool Update(T entity);
    bool Delete(T entity);
    Task<bool> AddRangeAsync(ICollection<T> entities);
    bool UpdateRange(ICollection<T> entities);
    bool DeleteRange(ICollection<T> entities);
}
