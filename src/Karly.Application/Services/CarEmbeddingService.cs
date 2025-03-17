#pragma warning disable SKEXP0001
using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;

namespace Karly.Application.Services;

public interface ICarEmbeddingService
{
    public Task CreateCarEmbeddingEntityAsync(CreateCarEmbeddingCommand command, CancellationToken cancellationToken = default);
    public Task<ReadOnlyMemory<float>?> GenerateEmbeddingAsync(CarDto carDto, CancellationToken cancellationToken = default);
    public Task<Dictionary<Guid, float[]>?> GenerateEmbeddingsAsync(CarsDto carsDto, CancellationToken cancellationToken = default);
}

public class CarEmbeddingService : ICarEmbeddingService
{
    private readonly ILogger<CarEmbeddingService> _logger;
    private readonly KarlyDbContext _karlyDbContext;
    private readonly ITextEmbeddingGenerationService _embeddingGenerationService;

    public CarEmbeddingService(ILogger<CarEmbeddingService> logger, KarlyDbContext karlyDbContext, ITextEmbeddingGenerationService embeddingGenerationService)
    {
        _logger = logger;
        _karlyDbContext = karlyDbContext;
        _embeddingGenerationService = embeddingGenerationService;
    }

    public async Task CreateCarEmbeddingEntityAsync(CreateCarEmbeddingCommand command, CancellationToken cancellationToken = default)
    {
        var carEmbedding = command.MapToCarEmbedding();
        await _karlyDbContext.CarEmbeddings.AddAsync(carEmbedding, cancellationToken);
    }
    
    public async Task<ReadOnlyMemory<float>?> GenerateEmbeddingAsync(CarDto carDto, CancellationToken cancellationToken = default)
    {
        var carIdAndEmbedding = await GenerateEmbeddingsAsync(new CarsDto{Items = [carDto]}, cancellationToken);
        return carIdAndEmbedding!.Single().Value;
    }
    
    public async Task<Dictionary<Guid, float[]>?> GenerateEmbeddingsAsync(CarsDto carsDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var inputsList = carsDto.Items
                .Select(car => new { id = car.Id, input = GenerateEmbeddingsInput(car) })
                .ToList();
            
            var embeddings = await _embeddingGenerationService.GenerateEmbeddingsAsync(inputsList.Select(kv => kv.input).ToList(), cancellationToken: cancellationToken);

            var embeddingsDictionary = new Dictionary<Guid, float[]>();
            for (var i = 0; i < embeddings.Count; i++)
            {
                var embedding = embeddings[i];
                embeddingsDictionary.Add(inputsList[i].id, embedding.ToArray());
            }
            
            return embeddingsDictionary;
        }
        catch (Exception exception)
        {
            _logger.LogError($"Error while generating embeddings: {exception.Message}");
            throw;
        }
    }
    
    private string GenerateEmbeddingsInput(CarDto carDto)
    {
        return $"Make: {carDto.Make}, Model: {carDto.Model}, Production year: {carDto.ProductionYear}, Mileage: {carDto.Mileage}, Is new: {carDto.IsNew}, Is Electric: {carDto.IsElectric}, Has automatic transmission: {carDto.HasAutomaticTransmission}, {carDto.Description}";
    }
}