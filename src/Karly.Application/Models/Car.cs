namespace Karly.Application.Models;

public class Car
{
    public required Guid Id { get; init; }
    public required string Model { get; init; }
    public required int ProductionYear { get; init; }
    public required decimal Price { get; init; }
    public string Description { get; set; }
    public CarEmbedding? CarEmbedding { get; set; }
}