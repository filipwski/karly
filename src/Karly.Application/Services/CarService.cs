#pragma warning disable SKEXP0001
using System.Text.Json;
using Karly.Api.Services;
using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Contracts.Commands;
using Karly.Contracts.Messages;
using Karly.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karly.Application.Services;

public interface ICarService
{
    public Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    public Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default);
    public Task<CarsDto> SearchAsync(string input, CancellationToken cancellationToken = default);
    public Task Generate(CancellationToken cancellationToken = default);
}

public class CarService : ICarService
{
    private readonly ILogger<ICarService> _logger;
    private readonly KarlyDbContext _dbContext;
    private readonly RabbitMqPublisherService _rabbitMqPublisherService;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;
    
    public CarService(ILogger<ICarService> logger, KarlyDbContext dbContext, RabbitMqPublisherService rabbitMqPublisherService, ITextEmbeddingGenerationService embeddingGenerationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _rabbitMqPublisherService = rabbitMqPublisherService;
    }
    
    public async Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) => (await _dbContext.Cars.FindAsync([id], cancellationToken))?.MapToDto();

    public async Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default) => (await _dbContext.Cars.ToListAsync(cancellationToken)).MapToDto();

    public async Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var car = command.MapToCar();
        await _dbContext.Cars.AddAsync(car, cancellationToken);
        return car.MapToDto();
    }

    public async Task Generate(CancellationToken cancellationToken = default)
    {
        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "ExampleCars.json");

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"Seed file not found: {jsonFilePath}");
        }

        var jsonString = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
        var carList = JsonSerializer.Deserialize<List<CreateCarMessage>>(jsonString);
        if (carList == null)
        {
            return;
        }

        _logger.LogInformation("Deleting all the old cars and embeddings");
        
        _dbContext.CarEmbeddings.RemoveRange(_dbContext.CarEmbeddings);
        _dbContext.Cars.RemoveRange(_dbContext.Cars);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        var tasks = carList.Select(car => _rabbitMqPublisherService.PublishCreateCarMessage(car, cancellationToken));
        await Task.WhenAll(tasks);

        await WaitForCarEmbeddingsAsync(cancellationToken);
    }
    
    private async Task WaitForCarEmbeddingsAsync(CancellationToken cancellationToken)
    {
        const int delayMilliseconds = 2000;
        const int maxRetries = 30;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            _logger.LogInformation("Waiting for car embeddings");
            
            var allCarsHaveEmbeddings = _dbContext.Cars.Count() == _dbContext.CarEmbeddings.Count();
            if (allCarsHaveEmbeddings)
            {
                return;
            }

            retryCount++;
            await Task.Delay(delayMilliseconds, cancellationToken);
        }
        
        _logger.LogInformation("Done. All car embeddings are applied.");

        throw new TimeoutException("Timed out waiting for car embeddings.");
    }

    public async Task<CarsDto> SearchAsync(string input, CancellationToken cancellationToken = default)
    {
        var queryEmbeddings = await _embeddingGenerationService.GenerateEmbeddingsAsync([input],
            cancellationToken: cancellationToken);
        var queryVector = new Vector(queryEmbeddings[0].ToArray());

        var cars = await _dbContext.Cars
            .Include(car => car.CarEmbedding)
            .OrderBy(car => car.CarEmbedding!.Embedding!.CosineDistance(queryVector))
            .Take(5)
            .ToListAsync(cancellationToken);

        return cars.MapToDto();
    }
}