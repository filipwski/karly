using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Karly.Application.Database;

public class KarlyDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public KarlyDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public DbSet<SampleEntity> SampleEntities { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SampleEntity>().ToTable(nameof(SampleEntity));
    }
}

public class SampleEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}