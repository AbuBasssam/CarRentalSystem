using Domain.Entities;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Implementations;

/// <summary>
/// Provides concrete implementation for car-related data access operations.
/// </summary>
public class CarRepository : GenericRepository<Car, int>, ICarRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CarRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CarRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<bool> IsPlateNumberENUniqueAsync(string plateNumberEN, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.PlateNumberEN == plateNumberEN);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsPlateNumberARUniqueAsync(string plateNumberAR, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.PlateNumberAR == plateNumberAR);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsVINUniqueAsync(string vin, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.VIN == vin);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    /// <inheritdoc />
    public IQueryable<Car> GetCarWithImages(int CarId) => GetByIdAsync(CarId).Include(c => c.Images);
}