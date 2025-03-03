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
    public Task RegenerateAsync(CancellationToken cancellationToken = default);
}

public class CarService : ICarService
{
    private readonly ILogger<ICarService> _logger;
    private readonly KarlyDbContext _dbContext;
    private readonly RabbitMqPublisherService _rabbitMqPublisherService;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;

    public CarService(ILogger<ICarService> logger, KarlyDbContext dbContext,
        RabbitMqPublisherService rabbitMqPublisherService, ITextEmbeddingGenerationService embeddingGenerationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _rabbitMqPublisherService = rabbitMqPublisherService;
        _embeddingGenerationService = embeddingGenerationService;
    }

    public async Task<CarDto?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await _dbContext.Cars.FindAsync([id], cancellationToken))?.MapToDto();

    public async Task<CarsDto> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await _dbContext.Cars.ToListAsync(cancellationToken)).MapToDto();

    public async Task<CarDto> Create(CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var car = command.MapToCar();
        await _dbContext.Cars.AddAsync(car, cancellationToken);
        return car.MapToDto();
    }

    public async Task RegenerateAsync(CancellationToken cancellationToken = default)
    {
        var createCarMessages = _dbContext.Cars.Select(ContractMapping.MapToCreateCarMessage).ToList();
        
        _logger.LogInformation("Deleting all the old cars and embeddings.");

        _dbContext.CarEmbeddings.RemoveRange(_dbContext.CarEmbeddings);
        _dbContext.Cars.RemoveRange(_dbContext.Cars);

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation($"New embeddings to regenerate: {createCarMessages.Count}.");

        var tasks = createCarMessages.Select(car => _rabbitMqPublisherService.PublishCreateCarMessage(car, cancellationToken)).ToList();
        await Task.WhenAll(tasks);

        await WaitForCarEmbeddingsAsync(tasks.Count, cancellationToken);

        await ExportCarsToJsonAsync(cancellationToken);
    }

    private async Task WaitForCarEmbeddingsAsync(int amountOfEmbeddings, CancellationToken cancellationToken)
    {
        const int delayMilliseconds = 5000;
        const int maxRetries = 30;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            _logger.LogInformation("Waiting for car embeddings.");

            var allTheEmbeddingsAreRegenerated = _dbContext.Cars.Count() == amountOfEmbeddings;
            if (allTheEmbeddingsAreRegenerated)
            {
                _logger.LogInformation("Done. All car embeddings are applied.");
                return;
            }

            retryCount++;
            await Task.Delay(delayMilliseconds, cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for car embeddings.");
    }

    private async Task ExportCarsToJsonAsync(CancellationToken cancellationToken)
    {
        var carsJsonModel = await _dbContext.Cars
            .Include(c => c.CarEmbedding)
            .Select(c => c.MapToCarJsonModel())
            .ToListAsync(cancellationToken);
        
        var jsonString = JsonSerializer.Serialize(carsJsonModel, new JsonSerializerOptions
        {
            WriteIndented = true 
        });

        var projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        var filePath = Path.Combine(projectRoot, "Karly.Application", "Database", "Resources", "ExampleCars.json");
        
        _logger.LogInformation($"Writing JSON to file: {filePath}");
        
        await File.WriteAllTextAsync(filePath, jsonString, cancellationToken);

        _logger.LogInformation("ExampleCars.json has been overriden successfully.");
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