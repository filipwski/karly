namespace Karly.Application.Options;

public class RabbitMqOptions
{
    public required string HostName { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required string CreateCarQueueName { get; init; }
    public required string RegenerateCarEmbeddingsQueueName { get; init; }
}