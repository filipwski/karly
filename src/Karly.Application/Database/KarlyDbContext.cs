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
                if (context.Set<Car>().FirstOrDefault() != null) return;
                
                var jsonString = File.ReadAllText(Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "../Karly.Application/Database/Resources/ExampleCars.json"));
                var carList = JsonSerializer.Deserialize<List<Car>>(jsonString);
                if (carList == null) return;

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