namespace Karly.Application.Models;

public class Car
{
    public Guid Id { get; init; }
    public string Model { get; init; }
    public int ProductionYear { get; init; }
    public decimal Price { get; init; }
    public string Description { get; set; }
    public CarEmbedding? CarEmbedding { get; set; }
}