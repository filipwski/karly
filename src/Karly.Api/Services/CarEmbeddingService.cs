#pragma warning disable SKEXP0001

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Karly.Api.Services;

public class CarEmbeddingService(Kernel kernel)
{
    private readonly ITextEmbeddingGenerationService _textEmbeddingGenerationService =
        kernel.GetRequiredService<ITextEmbeddingGenerationService>();

    public async Task GenerateEmbeddingsAsync()
    {
        try
        {
            var embedding = await _textEmbeddingGenerationService
                .GenerateEmbeddingsAsync(["Jestę bogę"]);

            Console.WriteLine(embedding);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while generating embeddings: {exception.Message}");
        }
    }
}