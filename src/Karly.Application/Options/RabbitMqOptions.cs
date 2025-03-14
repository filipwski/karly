namespace Karly.Application.Options;

public class RabbitMqOptions
{
    public required string HostName { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public RabbitMqQueueConfig CreateCarQueue { get; init; } = new();
    public RabbitMqQueueConfig RegenerateCarEmbeddingsQueue { get; init; } = new();
}

public class RabbitMqQueueConfig
{
    public string ExchangeName { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;
    public string RoutingKey { get; init; } = string.Empty;
    public string RetryExchangeName { get; init; } = string.Empty;
    public string RetryQueueName { get; init; } = string.Empty;
    public string RetryRoutingKey { get; init; } = string.Empty;
    public string DeadLetterExchangeName { get; init; } = string.Empty;
    public string DeadLetterQueueName { get; init; } = string.Empty;
    public string DeadLetterRoutingKey { get; init; } = string.Empty;
}