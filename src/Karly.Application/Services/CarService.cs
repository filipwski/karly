using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Application.Models;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
using Microsoft.EntityFrameworkCore;

namespace Karly.Application.Services;

public interface ICarService
{
    public Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default);
}

public class CarService(KarlyDbContext dbContext) : ICarService
{
    public async Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) => (await dbContext.Cars.FindAsync([id], cancellationToken))?.MapToDto();

    public async Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default) => (await dbContext.Cars.ToListAsync(cancellationToken)).MapToDto();

    public async Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var car = command.MapToCar();
        await dbContext.Cars.AddAsync(car, cancellationToken);
        return car.MapToDto();
    }
}