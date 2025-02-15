using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Application.Models;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
using Microsoft.EntityFrameworkCore;

namespace Karly.Application.Services;

public interface ICarService
{
    public Task<Car?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<IEnumerable<Car>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default);
}

public class CarService(KarlyDbContext dbContext) : ICarService
{
    public async Task<Car?> GetAsync(Guid id, CancellationToken cancellationToken = default) => await dbContext.Cars.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<Car>> GetAllAsync(CancellationToken cancellationToken = default) => await dbContext.Cars.ToListAsync(cancellationToken);

    public async Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var car = command.MapToCar();
        await dbContext.Cars.AddAsync(car, cancellationToken);
        return car.MapToDto();
    }
}