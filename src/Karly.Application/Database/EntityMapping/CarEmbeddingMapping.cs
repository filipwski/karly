using Karly.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karly.Application.Database.EntityMapping;

public class CarEmbeddingMapping : IEntityTypeConfiguration<CarEmbedding>
{
    public void Configure(EntityTypeBuilder<CarEmbedding> builder)
    {
        builder
            .ToTable("CarEmbeddings")
            .HasKey(x => x.Id);

        builder
            .HasOne(x => x.Car)
            .WithOne(x => x.CarEmbedding)
            .HasForeignKey<CarEmbedding>(x => x.CarId);

        builder
            .Property(x => x.Embedding)
            .HasColumnType("vector(1536)")
            .IsRequired();
    }
}