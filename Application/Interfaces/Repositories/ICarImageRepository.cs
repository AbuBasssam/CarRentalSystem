using Domain.Entities;

namespace Interfaces;

public interface ICarImageRepository : IGenericRepository<CarImage, int>, IScopedService
{
}