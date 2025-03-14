using Karly.Contracts.Responses;

namespace Karly.Contracts.Messages;

public record CreateCarMessage
{
    public string Type => nameof(CreateCarMessage);
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int ProductionYear { get; init; }
    public required decimal Price { get; init; }
    public required int Mileage { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsElectric { get; init; }
    public required bool HasAutomaticTransmission { get; init; }
    public required string Description { get; init; }
}

public record RegenerateCarEmbeddingsMessage
{
    public string Type => nameof(RegenerateCarEmbeddingsMessage);
    public required CarsDto CarsDto { get; init; }
}