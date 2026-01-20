using Domain.Entities;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Implementations;

/// <summary>
/// Repository for User entity operations
/// Handles user-specific database queries
/// </summary>
public class UserRepository : GenericRepository<User, int>, IUserRepository
{

    public UserRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets unverified users that have exceeded the retention period for deletion
    /// Used by UnverifiedUserCleanupService
    /// </summary>
    public async Task<List<User>> GetUnverifiedUsersForDeletionAsync(
        int retentionHours,
        int batchSize)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-retentionHours);

        return await _dbSet
            .Where(u =>!u.EmailConfirmed && u.CreatedAt <= cutoffTime)             
            .OrderBy(u => u.CreatedAt)                
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync();
    }
}