namespace Karly.Application.Models;

public record CarJsonModel
{
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int ProductionYear { get; init; }
    public required decimal Price { get; init; }
    public required int Mileage { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsElectric { get; init; }
    public required bool HasAutomaticTransmission { get; init; }
    public required string Description { get; set; }
    public float[] Embedding { get; init; }
}