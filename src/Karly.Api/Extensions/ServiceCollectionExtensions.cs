using Karly.Api.Services;

namespace Karly.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<ICarService, CarService>();
    }
}