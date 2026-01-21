using Microsoft.EntityFrameworkCore.Storage;

namespace Interfaces;

/// <summary>
/// Defines the Unit of Work pattern to manage database transactions 
/// and ensure atomic operations across multiple repositories.
/// </summary>
public interface IUnitOfWork : IScopedService
{
    /// <summary>
    /// Persists all changes made in the current context to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new database transaction.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction to the database.
    /// </summary>
    Task Commit();

    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    Task RollBack();
}
