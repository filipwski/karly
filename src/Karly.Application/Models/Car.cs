namespace Karly.Application.Models;

public class Car
{
    public Guid Id { get; init; }
    public string Model { get; init; }
    public int ProductionYear { get; init; }
    public string Description { get; init; }
    public float Price { get; init; }
}