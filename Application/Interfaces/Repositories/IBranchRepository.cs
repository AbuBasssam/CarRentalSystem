using Domain.Entities;

namespace Interfaces;

public interface IBranchRepository : IGenericRepository<Branch, int>
{
    /// <summary>
    /// Checks if a branch has any cars currently assigned to it.
    /// Used to prevent deletion of branches with active fleet.
    /// </summary>
    Task<bool> HasCarsAsync(int branchId, CancellationToken cancellationToken = default);
}