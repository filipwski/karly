namespace Karly.Contracts.Responses;

public class GetCarsDto
{
    public required IReadOnlyList<GetCarDto> Items { get; init; } = Enumerable.Empty<GetCarDto>().ToList();
}