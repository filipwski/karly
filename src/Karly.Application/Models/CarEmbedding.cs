using Pgvector;

namespace Karly.Application.Models;

public class CarEmbedding
{
    public Guid Id { get; init; }
    public required Guid CarId { get; init; }
    public Car? Car { get; init; }
    public Vector? Embedding { get; init; }
    
    public string[] EmbeddingInputs(Car car) => new[] { $"Make: {car.Make}, Model: {car.Model}, Production year: {car.ProductionYear}, Mileage: {car.Mileage}, Is new: {car.IsNew}, Is Electric: {car.IsElectric}, Has automatic transmission: {car.HasAutomaticTransmission}, {car.Description}" };
}