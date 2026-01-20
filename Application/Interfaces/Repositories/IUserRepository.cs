using Domain.Entities;

namespace Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// Extends basic CRUD with user-specific queries
/// </summary>
public interface IUserRepository : IGenericRepository<User, int>
{
    /// <summary>
    /// Gets unverified users that have exceeded the retention period for deletion
    /// Returns users where:
    /// - EmailConfirmed = false
    /// - CreatedAt is older than retentionHours
    /// </summary>
    /// <param name="retentionHours">Number of hours to keep unverified users before deletion</param>
    /// <param name="batchSize">Maximum number of users to return in one batch</param>
    /// <returns>List of unverified users ready for deletion</returns>
    Task<List<User>> GetUnverifiedUsersForDeletionAsync(int retentionHours, int batchSize);
}