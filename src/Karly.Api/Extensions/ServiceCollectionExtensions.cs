#pragma warning disable SKEXP0010

using Karly.Api.Services;
using Microsoft.SemanticKernel;

namespace Karly.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<ICarEmbeddingService, CarEmbeddingService>();
        services.AddOpenAiTextEmbeddingGeneration(configuration);
    }

    private static void AddOpenAiTextEmbeddingGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiKey = configuration.GetValue<string>("OpenAiKey");
        if (openAiKey == null) throw new Exception("Karly:OpenAiKey is required");

        services.AddOpenAITextEmbeddingGeneration("text-embedding-ada-002", openAiKey);
        services.AddKernel();
    }
}