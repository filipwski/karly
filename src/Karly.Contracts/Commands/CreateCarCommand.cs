namespace Karly.Contracts.Commands;

public record CreateCarCommand
{
    public string Make { get; init; }
    public string Model { get; init; }
    public int ProductionYear { get; init; }
    public decimal Price { get; init; }
    public int Mileage { get; init; }
    public bool IsNew { get; init; }
    public bool IsElectric { get; init; }
    public bool HasAutomaticTransmission { get; init; }
    public string Description { get; set; }
}