using Microsoft.EntityFrameworkCore;

namespace Karly.Application.Database;

public class KarlyDbContext : DbContext
{
    public DbSet<SampleEntity> SampleEntities { get; set; }
}

public class SampleEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}