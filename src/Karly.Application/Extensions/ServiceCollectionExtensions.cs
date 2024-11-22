using Karly.Application.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karly.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KarlyDbContext");
        services.AddDbContext<KarlyDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}