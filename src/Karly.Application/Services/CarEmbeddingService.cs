using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
using Microsoft.SemanticKernel.Embeddings;
#pragma warning disable SKEXP0001

namespace Karly.Application.Services;

public interface ICarEmbeddingService
{
    public Task CreateCarEmbeddingEntityAsync(CreateCarEmbeddingCommand command, CancellationToken cancellationToken = default);
    public Task<IList<ReadOnlyMemory<float>>?> GenerateEmbeddingsAsync(CarDto carDto, CancellationToken cancellationToken = default);
}

public class CarEmbeddingService(KarlyDbContext karlyDbContext, ITextEmbeddingGenerationService embeddingGenerationService) : ICarEmbeddingService
{
    public async Task CreateCarEmbeddingEntityAsync(CreateCarEmbeddingCommand command, CancellationToken cancellationToken = default)
    {
        var carEmbedding = command.MapToCarEmbedding();
        await karlyDbContext.CarEmbeddings.AddAsync(carEmbedding, cancellationToken);
    }
    
    public async Task<IList<ReadOnlyMemory<float>>?> GenerateEmbeddingsAsync(CarDto carDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var input = GenerateEmbeddingsInput(carDto);
            var embeddings = await embeddingGenerationService.GenerateEmbeddingsAsync(input, cancellationToken: cancellationToken);
            return embeddings;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while generating embeddings: {exception.Message}");
            return null;
        }
    }
    
    private string[] GenerateEmbeddingsInput(CarDto carDto)
    {
        return new[] { $"Make: {carDto.Make}, Model: {carDto.Model}, Production year: {carDto.ProductionYear}, Mileage: {carDto.Mileage}, Is new: {carDto.IsNew}, Is Electric: {carDto.IsElectric}, Has automatic transmission: {carDto.HasAutomaticTransmission}, {carDto.Description}" };
    }
}