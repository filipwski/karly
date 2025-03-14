using Pgvector;

namespace Karly.Application.Models;

public class CarEmbedding : IAuditable
{
    public Guid Id { get; init; }
    public Guid CarId { get; init; }
    public Car? Car { get; init; }
    public required Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    
    public string[] EmbeddingInputs(Car car) => new[] { $"Make: {car.Make}, Model: {car.Model}, Production year: {car.ProductionYear}, Mileage: {car.Mileage}, Is new: {car.IsNew}, Is Electric: {car.IsElectric}, Has automatic transmission: {car.HasAutomaticTransmission}, {car.Description}" };
}