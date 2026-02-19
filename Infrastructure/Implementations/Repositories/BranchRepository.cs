using Domain.Entities;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 
/// </summary>
public class BranchRepository : GenericRepository<Branch, int>, IBranchRepository, IScopedService
{
    public BranchRepository(AppDbContext context) : base(context) { }

    public async Task<bool> HasCarsAsync(int branchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Id == branchId)
            .SelectMany(b => b.Cars)
            .AnyAsync(cancellationToken);
    }
}