namespace Karly.Contracts.Responses;

public record GetCarsDto
{
    public required IReadOnlyList<CarDto> Items { get; init; } = Enumerable.Empty<CarDto>().ToList();
}