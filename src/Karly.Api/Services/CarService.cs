using Karly.Application.Database;
using Karly.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Karly.Api.Services;

public interface ICarService
{
    public Task<Car?> GetAsync(Guid id);
    public Task<IEnumerable<Car>> GetAllAsync();
}

public class CarService(KarlyDbContext dbContext) : ICarService
{
    public async Task<Car?> GetAsync(Guid id) => await dbContext.Cars.FindAsync(id);

    public async Task<IEnumerable<Car>> GetAllAsync() => await dbContext.Cars.ToListAsync();
}