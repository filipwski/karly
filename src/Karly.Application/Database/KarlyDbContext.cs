using System.Text.Json;
using Karly.Application.Database.EntityMapping;
using Karly.Application.Models;
using Karly.Contracts.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Karly.Application.Database;

public class KarlyDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public KarlyDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarEmbedding> CarEmbeddings => Set<CarEmbedding>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            _configuration.GetConnectionString("KarlyDbContext"),
            o => o.UseVector());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfiguration(new CarMapping());
        modelBuilder.ApplyConfiguration(new CarEmbeddingMapping());
    }
}