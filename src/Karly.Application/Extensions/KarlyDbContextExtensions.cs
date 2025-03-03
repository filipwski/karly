using System.Text.Json;
using Karly.Application.Database;
using Karly.Application.Models;
using Karly.Contracts.Utils;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Karly.Application.Extensions;

public static class KarlyDbContextExtensions
{
    public static void EnsureSeedData(this KarlyDbContext dbContext, ILogger logger)
    {
        if (dbContext.Set<Car>().FirstOrDefault() != null) return;

        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "ExampleCars.json");

        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"Seed file not found: {jsonFilePath}");
        }

        var jsonString = File.ReadAllText(jsonFilePath);
        var carsJsonDto = JsonSerializer.Deserialize<List<CarJsonDto>>(jsonString);
        if (carsJsonDto == null) return;

        logger.LogInformation($"Seeding {carsJsonDto.Count} cars from {jsonFilePath} path...");

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

        dbContext.Set<Car>().AddRange(carList);
        dbContext.SaveChanges();

        logger.LogInformation("Seeding complete.");
    }
}