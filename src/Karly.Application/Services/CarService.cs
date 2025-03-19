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
    Task GenerateAndUpdateDescriptionsAsync(CarsDto carsDto, CancellationToken cancellationToken = default);
    public Task<CarDto?> GenerateAndUpdateDescriptionAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CarService : ICarService
{
    private readonly ILogger<ICarService> _logger;
    private readonly KarlyDbContext _dbContext;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;
    private readonly IChatCompletionService _chatCompletionService;

    public CarService(ILogger<ICarService> logger, KarlyDbContext dbContext,
        ITextEmbeddingGenerationService embeddingGenerationService,
        IChatCompletionService chatCompletionService)
    {
        _logger = logger;
        _dbContext = dbContext;
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
    
    public async Task<CarDto?> GenerateAndUpdateDescriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await _dbContext.Cars.FindAsync([id], cancellationToken);
        if (car == null) return null;

        var carIdAndDescription = await GenerateDescriptionAsync(car.MapToDto(), cancellationToken);
        
        car.Description = carIdAndDescription.Description;
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return car.MapToDto();
    }
    
    public async Task GenerateAndUpdateDescriptionsAsync(CarsDto carsDto, CancellationToken cancellationToken = default)
    {
        var tasks = carsDto.Items.Select(car => GenerateDescriptionAsync(car, cancellationToken));
        var carIdAndDescriptionArray = await Task.WhenAll(tasks);
        
        foreach (var carIdAndDescription in carIdAndDescriptionArray)
        {
            var car = await _dbContext.Cars.FindAsync([carIdAndDescription.Item1], cancellationToken);
            if (car == null)
            {
                continue;
            }
            car.Description = carIdAndDescription.Item2;
        }
    }

    private async Task<(Guid Id, string Description)> GenerateDescriptionAsync(CarDto carDto, CancellationToken cancellationToken = default)
    {
        ChatHistory history = [];
        history.AddUserMessage(
            """
            Generate a detailed, technical, and context-rich description for a car in a catalog. Follow these rules:
            1. **Avoid negative terms** for non-EVs (e.g., do NOT write "electric: false", "not electric", "no EV components").
            2. **Avoid these words: audio, focus**, and any others that contain or are synonyms of popular car brands or models.
            3. **Focus on unambiguous keywords**:
               - For non-EVs: Use terms like "gasoline", "diesel", "hybrid (non plug-in)", "MPG", "engine displacement", "fuel efficiency".
               - For EVs: Use "EV", "electric motor", "battery capacity", "DC charging", "zero emissions".
            4. **Include**:
               - Engine specs (e.g., "2.0L inline-4", "turbocharged").
               - Transmission type (e.g., "automatic CVT", "manual 6-speed").
               - Fuel/energy consumption (e.g., "32 MPG city", "4.5L/100km", "400 km range (WLTP)").
               - Key features (e.g., "Apple CarPlay", "adaptive cruise control", "panoramic roof").
               - Use-case context (e.g., "ideal for city driving", "long-distance comfort", "family-friendly").
               - Do not include any line breaks.
               - Do not include any quote characters. The answer should be raw text.
            5. **Structure**:  
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

        history.AddUserMessage(JsonSerializer.Serialize(carDto));

        var response = await _chatCompletionService.GetChatMessageContentsAsync(history, cancellationToken: cancellationToken);

        var description = response[^1].ToString();
        
        return (carDto.Id, description);
    }

    public async Task<CarsDto> SearchAsync(string input, CancellationToken cancellationToken = default)
    {
        var queryEmbeddings = await _embeddingGenerationService.GenerateEmbeddingsAsync([input],
            cancellationToken: cancellationToken);
        var queryVector = new Vector(queryEmbeddings[0].ToArray());

        var carsQuery = _dbContext.Cars.Include(car => car.CarEmbedding);

        var result = await carsQuery
            .OrderBy(car => car.CarEmbedding!.Embedding!.CosineDistance(queryVector))
            .Take(5)
            .ToListAsync(cancellationToken);
        
        var carsDto = result.Select(c => c.MapToDto(CosineDistance(c.CarEmbedding!.Embedding!.ToArray(), queryVector.ToArray()))).ToList();

        var firstCarCosineDistance = carsDto.First().Distance;
        
        return carsDto.Where(car => car.Distance - firstCarCosineDistance < 0.1 && car.Distance < 0.85).MapToDto();
    }
    
    private static double CosineDistance(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must be of the same length");    
        }
        
        double dot = 0.0, magA = 0.0, magB = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return 1 - dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}