using Karly.Application.Database.EntityMapping;
using Karly.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Karly.Application.Database;

public class KarlyDbContext(IConfiguration configuration) : DbContext
{
    public DbSet<Car> Cars => Set<Car>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("KarlyDbContext"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CarMapping());
    }
}
