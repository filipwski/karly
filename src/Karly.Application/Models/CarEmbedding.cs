using Pgvector;

namespace Karly.Application.Models;

public class CarEmbedding : IAuditable
{
    public Guid Id { get; init; }
    public Guid CarId { get; init; }
    public Car? Car { get; init; }
    public required Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
}