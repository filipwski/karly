#pragma warning disable SKEXP0010
using Karly.Application.Database;
using Karly.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Karly.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KarlyDbContext");
        services.AddDbContext<KarlyDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
    
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RabbitMqPublisherService>();
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<ICarEmbeddingService, CarEmbeddingService>();
        services.AddSemanticKernelServices(configuration);
    }

    private static void AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiKey = configuration.GetValue<string>("OpenAiKey");
        if (openAiKey == null) throw new Exception("OpenAiKey is required");

        services.AddKernel();
        services.AddOpenAIChatCompletion("gpt-4o", openAiKey);
        services.AddOpenAITextEmbeddingGeneration("text-embedding-3-small", openAiKey);
    }
}