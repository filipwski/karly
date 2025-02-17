namespace Karly.Contracts.Responses;

public record CarsDto
{
    public required IReadOnlyList<CarDto> Items { get; init; } = Enumerable.Empty<CarDto>().ToList();
}