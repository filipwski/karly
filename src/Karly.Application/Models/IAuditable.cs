namespace Karly.Application.Models;

public interface IAuditable
{
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; }
}