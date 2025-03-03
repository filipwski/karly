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
    private readonly ILogger<KarlyDbContext> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    
    public KarlyDbContext(ILogger<KarlyDbContext> logger, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }
    
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarEmbedding> CarEmbeddings => Set<CarEmbedding>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            _configuration.GetConnectionString("KarlyDbContext"),
            o => o.UseVector());

        if (_hostEnvironment.IsDevelopment())
        {
            optionsBuilder.UseSeeding((context, _) =>
            {
                if (context.Set<Car>().FirstOrDefault() != null) return;

                var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "ExampleCars.json");

                if (!File.Exists(jsonFilePath))
                {
                    throw new FileNotFoundException($"Seed file not found: {jsonFilePath}");
                }

                var jsonString = File.ReadAllText(jsonFilePath);
                var carsJsonDto = JsonSerializer.Deserialize<List<CarJsonDto>>(jsonString);
                if (carsJsonDto == null) return;
                
                _logger.LogInformation(jsonString);
                _logger.LogInformation($"Seeding {carsJsonDto.Count} cars from {jsonFilePath} path...");
                
                var carList = carsJsonDto.Select(dto => new Car
                {
                    Model = dto.Model,
                    Description = dto.Description,
                    Price = dto.Price,
                    ProductionYear = dto.ProductionYear,
                    HasAutomaticTransmission = dto.HasAutomaticTransmission,
                    IsElectric = dto.IsElectric,
                    IsNew = dto.IsNew,
                    Make = dto.Make,
                    Mileage = dto.Mileage,
                    CarEmbedding = new CarEmbedding
                    {
                        Embedding = new Vector(dto.Embedding.ToArray())
                    }
                }).ToList();

                context.Set<Car>().AddRange(carList);
                context.SaveChanges();
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfiguration(new CarMapping());
        modelBuilder.ApplyConfiguration(new CarEmbeddingMapping());
    }
}