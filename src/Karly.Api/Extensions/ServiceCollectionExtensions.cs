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
        services.AddSemanticKernelServices(configuration);
    }

    private static void AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiKey = configuration.GetValue<string>("OpenAiKey");
        if (openAiKey == null) throw new Exception("OpenAiKey is required");

        services.AddKernel();
        services.AddOpenAITextEmbeddingGeneration("text-embedding-ada-002", openAiKey);
    }
}