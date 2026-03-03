using Domain.Entities;
using Infrastructure;
using Infrastructure.Repositories;
using Interfaces;

namespace Implementations;

public class CarImageRepository : GenericRepository<CarImage, int>, ICarImageRepository
{
    public CarImageRepository(AppDbContext context) : base(context) { }
}