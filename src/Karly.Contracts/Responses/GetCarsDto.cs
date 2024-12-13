namespace Karly.Contracts.Responses;

public record GetCarsDto
{
    public required IReadOnlyList<GetCarDto> Items { get; init; } = Enumerable.Empty<GetCarDto>().ToList();
}