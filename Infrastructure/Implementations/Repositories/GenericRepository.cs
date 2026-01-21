using Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// A generic repository implementation providing standard CRUD (Create, Read, Update, Delete) operations 
/// for any entity type. This promotes code reuse and ensures a consistent data access pattern across the application.
/// </summary>
/// <typeparam name="T">The type of the entity. Must be a class and implement <see cref="IEntity{TKey}"/>.</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity (e.g., int, Guid, string).</typeparam>
public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class, IEntity<TKey>
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The primary key of the entity.</param>
    /// <returns>An IQueryable containing the entity if found.</returns>
    public IQueryable<T> GetByIdAsync(TKey id)
    {

        return _dbSet.Where(entity => entity.Id!.Equals(id));
    }

    /// <summary>
    /// Retrieves all entities from the table without tracking changes (read-only).
    /// </summary>
    /// <returns>An IQueryable of the entities.</returns>
    public IQueryable<T> GetTableNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    /// <summary>
    /// Retrieves all entities from the table with change tracking enabled.
    /// </summary>
    /// <returns>An IQueryable of the entities.</returns>
    public IQueryable<T> GetTableAsTracking()
    {
        return _dbSet.AsTracking();
    }

    /// <summary>
    /// Checks if an entity exists in the database by its ID.
    /// </summary>
    /// <param name="id">The primary key to check.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    public async Task<bool> IsExistsByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id) != null;
    }

    /// <summary>
    /// Adds a new entity to the database context.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The primary key of the newly added entity.</returns>
    public async Task<TKey> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// Marks an entity as modified in the database context.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>True if the operation was successful.</returns>
    public bool Update(T entity)
    {
        _dbSet.Update(entity);
        return true;
    }

    /// <summary>
    /// Marks an entity for deletion from the database.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>True if the operation was successful.</returns>
    public bool Delete(T entity)
    {
        _dbSet.Remove(entity);
        return true;
    }

    /// <summary>
    /// Adds a collection of entities to the database context in a single batch operation.
    /// </summary>
    /// <param name="entities">The collection of entities to be added.</param>
    /// <returns>A boolean value indicating the success of the operation.</returns>
    public async Task<bool> AddRangeAsync(ICollection<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        return true;
    }

    /// <summary>
    /// Marks a collection of entities as modified in the database context.
    /// </summary>
    /// <param name="entities">The collection of entities to be updated.</param>
    /// <returns>A boolean value indicating the success of the operation.</returns>
    public bool UpdateRange(ICollection<T> entities)
    {
        _dbSet.UpdateRange(entities);
        return true;
    }

    /// <summary>
    /// Marks a collection of entities for deletion from the database.
    /// </summary>
    /// <param name="entities">The collection of entities to be removed.</param>
    /// <returns>A boolean value indicating the success of the operation.</returns>
    public bool DeleteRange(ICollection<T> entities)
    {
        _dbSet.RemoveRange(entities);
        return true;
    }

    /// <summary>
    /// Retrieves a specific page of entities using offset-based pagination.
    /// Results are ordered by the entity ID and retrieved without change tracking.
    /// </summary>
    /// <param name="PageNumber">The page index to retrieve (defaults to 1).</param>
    /// <returns>An IQueryable representing the requested page with a fixed size of 10 records.</returns>
    public virtual IQueryable<T> GetPage(int PageNumber = 1)
    {
        return _dbSet.AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip((PageNumber - 1) * 10)
            .Take(10);
    }
}
