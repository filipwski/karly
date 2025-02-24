using System.Text.Json;
using Karly.Application.Database.EntityMapping;
using Karly.Application.Models;
using Karly.Contracts.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Pgvector;

namespace Karly.Application.Database;

public class KarlyDbContext(IConfiguration configuration, IHostEnvironment hostEnvironment) : DbContext
{
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarEmbedding> CarEmbeddings => Set<CarEmbedding>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            configuration.GetConnectionString("KarlyDbContext"),
            o => o.UseVector());

        if (hostEnvironment.IsDevelopment())
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
                var carsJsonDto = JsonSerializer.Deserialize<List<CarsJsonDto>>(jsonString);
                if (carsJsonDto == null) return;

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