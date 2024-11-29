using System.Text.Json;
using Karly.Application.Database.EntityMapping;
using Karly.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Karly.Application.Database;

public class KarlyDbContext(IConfiguration configuration, IHostEnvironment hostEnvironment) : DbContext
{
    public DbSet<Car> Cars => Set<Car>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("KarlyDbContext"));

        if (hostEnvironment.IsDevelopment())
        {
            optionsBuilder.UseSeeding((context, _) =>
            {
                var car = context.Set<Car>().First();
                if (car != null) return;
                
                var jsonPath = Path.Combine(
                    Directory
                        .GetParent(Directory.GetCurrentDirectory())
                        .GetDirectories()
                        .First(dir => dir.Name.Contains("Application")).FullName,
                    "Database/Resources/ExampleCars.json");
                var jsonString = File.ReadAllText(jsonPath);
                var carList = JsonSerializer.Deserialize<List<Car>>(jsonString);

                context.Set<Car>().AddRange(carList);
                context.SaveChanges();
            });
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CarMapping());
    }
}