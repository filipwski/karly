namespace Karly.Application.Models;

public class CarEmbedding
{
    public required Guid Id { get; init; }
    public required Guid CarId { get; init; }
    public required Car Car { get; init; }
}