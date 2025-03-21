using Karly.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karly.Application.Database.EntityMapping;

public class CarMapping : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder
            .ToTable("Cars")
            .HasKey(x => x.Id);
        
        builder
            .Property(x => x.Make)
            .HasColumnType("text")
            .HasMaxLength(30)
            .IsRequired();
        
        builder
            .Property(x => x.Model)
            .HasColumnType("text")
            .HasMaxLength(30)
            .IsRequired();
        
        builder
            .Property(x => x.ProductionYear)
            .HasColumnType("integer")
            .IsRequired();
        
        builder
            .Property(x => x.Mileage)
            .HasColumnType("integer")
            .IsRequired();
        
        builder
            .Property(x => x.IsNew)
            .HasColumnType("boolean")
            .IsRequired();
        
        builder
            .Property(x => x.IsElectric)
            .HasColumnType("boolean")
            .IsRequired();
        
        builder
            .Property(x => x.HasAutomaticTransmission)
            .HasColumnType("boolean")
            .IsRequired();
        
        builder
            .Property(x => x.Description)
            .HasColumnType("text")
            .HasMaxLength(250)
            .IsRequired();
        
        builder
            .Property(x => x.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
    }
}