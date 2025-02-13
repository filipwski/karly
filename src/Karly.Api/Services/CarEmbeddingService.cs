#pragma warning disable SKEXP0001

using Microsoft.SemanticKernel.Embeddings;

namespace Karly.Api.Services;

public interface ICarEmbeddingService
{
    public Task<IList<ReadOnlyMemory<float>>?> GenerateEmbeddingsAsync(string[] input);
}

public class CarEmbeddingService(ITextEmbeddingGenerationService embeddingGenerationService) : ICarEmbeddingService
{
    public async Task<IList<ReadOnlyMemory<float>>?> GenerateEmbeddingsAsync(string[] input)
    {
        try
        {
            var embeddings = await embeddingGenerationService
                .GenerateEmbeddingsAsync(input);
            return embeddings;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while generating embeddings: {exception.Message}");
            return null;
        }
    }
}