using Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementation of the Unit of Work pattern to coordinate the work of multiple repositories 
/// and share a single database context to ensure data integrity and transaction atomicity.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Saves all changes made in the current transaction to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    ///<returns>A task representing the asynchronous save operation.</returns>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Starts a new database transaction to ensure atomicity.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The initiated database transaction object.</returns>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction, making all changes permanent.
    /// </summary>
    public async Task Commit()
    {
        await _context.Database.CommitTransactionAsync();
    }

    /// <summary>
    /// Rolls back the current transaction, undoing all changes since the transaction started.
    /// </summary>
    public async Task RollBack()
    {
        await _context.Database.RollbackTransactionAsync();

    }

}

