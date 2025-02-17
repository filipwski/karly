using Pgvector;

namespace Karly.Contracts.Commands;

public class CreateCarEmbeddingCommand
{
    public required Guid CarId { get; init; }
    public Vector Embedding { get; init; }
}