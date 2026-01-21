using Microsoft.EntityFrameworkCore.Storage;

namespace Interfaces;

public interface IUnitOfWork : IScopedService
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task Commit();
    Task RollBack();
}
