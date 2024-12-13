namespace Karly.Contracts.Responses;

public record GetCarDto
{
    public required Guid Id { get; init; }
    public required string Model { get; init; }
    public required int ProductionYear { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
}