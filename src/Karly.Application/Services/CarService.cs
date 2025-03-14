#pragma warning disable SKEXP0001
using System.Text.Json;
using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
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
    public Task<CarDto?> GenerateDescriptionAsync(Guid id, CancellationToken cancellationToken = default);
    public Task RegenerateAsync(CancellationToken cancellationToken = default);
}

public class CarService : ICarService
{
    private readonly ILogger<ICarService> _logger;
    private readonly KarlyDbContext _dbContext;
    private readonly RabbitMqPublisherService _rabbitMqPublisherService;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;
    private readonly IChatCompletionService _chatCompletionService;

    public CarService(ILogger<ICarService> logger, KarlyDbContext dbContext,
        RabbitMqPublisherService rabbitMqPublisherService, ITextEmbeddingGenerationService embeddingGenerationService,
        IChatCompletionService chatCompletionService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _rabbitMqPublisherService = rabbitMqPublisherService;
        _embeddingGenerationService = embeddingGenerationService;
        _chatCompletionService = chatCompletionService;
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

    public async Task<CarDto?> GenerateDescriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await _dbContext.Cars.FindAsync([id], cancellationToken);
        if (car == null) return null;

        ChatHistory history = [];
        history.AddUserMessage(
            """
            Generate a detailed, technical, and context-rich description for a car in a catalog. Follow these rules:
            1. **Avoid negative terms** for non-EVs (e.g., do NOT write "electric: false", "not electric", "no EV components").  
            2. **Focus on unambiguous keywords**:
               - For non-EVs: Use terms like "gasoline", "diesel", "hybrid (non plug-in)", "MPG", "engine displacement", "fuel efficiency".
               - For EVs: Use "EV", "electric motor", "battery capacity", "DC charging", "zero emissions".
            3. **Include**:
               - Engine specs (e.g., "2.0L inline-4", "turbocharged").
               - Transmission type (e.g., "automatic CVT", "manual 6-speed").
               - Fuel/energy consumption (e.g., "32 MPG city", "4.5L/100km", "400 km range (WLTP)").
               - Key features (e.g., "Apple CarPlay", "adaptive cruise control", "panoramic roof").
               - Use-case context (e.g., "ideal for city driving", "long-distance comfort", "family-friendly").
               - Do not include any line breaks.
               - Do not include any quote characters. The answer should be raw text.
            4. **Structure**:  
               [Make] [Model] [Year] ([Engine Type]) – [New/Used] [Body Type].  
               [Core attributes: fuel type, transmission, drivetrain].  
               [Specs: fuel efficiency, mileage (if used)].  
               [Notable features].  
               [Use-case summary].
               [Client type recommendation].
            **Example for non-EV**:
            "Honda Civic 2019 (2.0L Gasoline) – Used Sedan. Conventional gasoline-powered sedan with a 2.0L inline-4 engine, automatic CVT transmission, and front-wheel drive (FWD). Fuel efficiency: 32 MPG city / 42 MPG highway. Mileage: 66,982 miles. Features: Honda Sensing Suite, 7-inch touchscreen with Apple CarPlay, dual-zone climate control. Ideal for daily commuting and long trips, offering reliability and low maintenance costs."
            **Input Data**:
            """);

        history.AddUserMessage(JsonSerializer.Serialize(car));

        var response =
            await _chatCompletionService.GetChatMessageContentsAsync(history, cancellationToken: cancellationToken);

        var description = response[^1].ToString();
        car.Description = description;

        _dbContext.Cars.Update(car);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
        const int delayMilliseconds = 10000;
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