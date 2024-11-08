using Karly.Application.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Karly.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlDbContext(this IServiceCollection services)
    {
        services.AddDbContext<KarlyDbContext>();
        return services;
    }
}