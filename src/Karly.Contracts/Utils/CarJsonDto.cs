namespace Karly.Contracts.Utils;

public record CarJsonDto
{
    public required string Model { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int ProductionYear { get; init; }
    public required bool HasAutomaticTransmission { get; init; }
    public required bool IsElectric { get; init; }
    public required bool IsNew { get; init; }
    public required string Make { get; init; }
    public required int Mileage { get; init; }
    public required float[] Embedding { get; init; }
}