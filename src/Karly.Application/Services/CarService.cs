#pragma warning disable SKEXP0001
using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Karly.Application.Services;

public interface ICarService
{
    public Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default);
    public Task<CarsDto> SearchAsync(string input, CancellationToken cancellationToken = default);
}

public class CarService(KarlyDbContext dbContext, ITextEmbeddingGenerationService embeddingGenerationService)
    : ICarService
{
    public async Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) => (await dbContext.Cars.FindAsync([id], cancellationToken))?.MapToDto();

    public async Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default) => (await dbContext.Cars.ToListAsync(cancellationToken)).MapToDto();

    public async Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var car = command.MapToCar();
        await dbContext.Cars.AddAsync(car, cancellationToken);
        return car.MapToDto();
    }

    public async Task<CarsDto> SearchAsync(string input, CancellationToken cancellationToken = default)
    {
        const double threshold = 0.9;

        var queryEmbeddings = await embeddingGenerationService.GenerateEmbeddingsAsync([input],
            cancellationToken: cancellationToken);
        var queryVector = new Vector(queryEmbeddings[0].ToArray());

        Console.WriteLine(queryVector.ToString());

        var cars = await dbContext.Cars
            .Include(car => car.CarEmbedding)
            .OrderBy(car => car.CarEmbedding!.Embedding!.L2Distance(queryVector))
            .Take(5)
            .ToListAsync(cancellationToken);

        return cars.MapToDto();
    }
}