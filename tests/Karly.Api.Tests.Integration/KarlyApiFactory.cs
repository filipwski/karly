using Karly.Application.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Karly.Api.Tests.Integration;

public class KarlyApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:17.1")
            .WithEntrypoint("sh", "-c", "apt-get update && apt-get install -y postgresql-17-pgvector && exec docker-entrypoint.sh postgres")
            .WithDatabase("karly")
            .WithUsername("postgres")
            .WithPassword("test123")
            .WithPortBinding(5432)
            .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(KarlyDbContext));
            services.AddDbContext<KarlyDbContext>(optionsBuilder =>
                optionsBuilder.UseNpgsql(_dbContainer.GetConnectionString(), o => o.UseVector()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}