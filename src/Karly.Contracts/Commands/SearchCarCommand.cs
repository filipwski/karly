namespace Karly.Contracts.Commands;

public record SearchCarCommand
{
    public required string Input { get; init; }
}