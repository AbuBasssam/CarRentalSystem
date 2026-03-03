using Domain.Entities;

namespace Interfaces;

/// <summary>
/// Repository interface for managing <see cref="Car"/> entities and performing car-specific data operations.
/// </summary>
public interface ICarRepository : IGenericRepository<Car, int>, IScopedService
{
    /// <summary>
    /// Checks if the English plate number is unique across all cars.
    /// </summary>
    /// <param name="plateNumberEN">The English plate number to check.</param>
    /// <param name="excludeId">Optional car ID to exclude from the check (useful during updates).</param>
    /// <returns>True if the plate number is unique; otherwise, false.</returns>
    Task<bool> IsPlateNumberENUniqueAsync(string plateNumberEN, int? excludeId = null);

    /// <summary>
    /// Checks if the Arabic plate number is unique across all cars.
    /// </summary>
    /// <param name="plateNumberAR">The Arabic plate number to check.</param>
    /// <param name="excludeId">Optional car ID to exclude from the check (useful during updates).</param>
    /// <returns>True if the plate number is unique; otherwise, false.</returns>
    Task<bool> IsPlateNumberARUniqueAsync(string plateNumberAR, int? excludeId = null);

    /// <summary>
    /// Checks if the Vehicle Identification Number (VIN) is unique across all cars.
    /// </summary>
    /// <param name="vin">The VIN to check.</param>
    /// <param name="excludeId">Optional car ID to exclude from the check (useful during updates).</param>
    /// <returns>True if the VIN is unique; otherwise, false.</returns>
    Task<bool> IsVINUniqueAsync(string vin, int? excludeId = null);
}