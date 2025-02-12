#pragma warning disable SKEXP0001

using Microsoft.SemanticKernel.Embeddings;

namespace Karly.Api.Services;

public interface ICarEmbeddingService
{
    public Task GenerateEmbeddingsAsync();
}

public class CarEmbeddingService(ITextEmbeddingGenerationService embeddingGenerationService) : ICarEmbeddingService
{
    public async Task GenerateEmbeddingsAsync()
    {
        try
        {
            var embedding = await embeddingGenerationService
                .GenerateEmbeddingsAsync(["Jestę bogę"]);

            Console.WriteLine(embedding);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while generating embeddings: {exception.Message}");
        }
    }
}