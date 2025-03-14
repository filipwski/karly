using System.Text;
using System.Text.Json;
using Karly.Application.Options;
using Karly.Contracts.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Karly.Application.Services;

public class RabbitMqPublisherService
{
    private readonly ILogger<RabbitMqPublisherService> _logger;
    private readonly RabbitMqOptions _options;
    private readonly ConnectionFactory _factory;

    public RabbitMqPublisherService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisherService> logger)
    {
        _logger = logger;
        _options = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };
    }

    public async Task PublishCreateCarMessage(CreateCarMessage message, CancellationToken cancellationToken = default)
    {
        await PublishMessage(message, _options.CreateCarQueueName, cancellationToken);
    }

    public async Task PublishRegenerateCarEmbeddingsMessage(RegenerateCarEmbeddingsMessage message, CancellationToken cancellationToken = default)
    {
        await PublishMessage(message, _options.RegenerateCarEmbeddingsQueueName, cancellationToken);
    }

    private async Task PublishMessage<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false,
            autoDelete: false, arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx_exchange" },
                { "x-dead-letter-routing-key", $"{queueName}.dlx" }
            }!, cancellationToken: cancellationToken);

        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        var addr = new PublicationAddress(exchangeType: ExchangeType.Direct, exchangeName: "", routingKey: queueName);

        await channel.BasicPublishAsync(addr, props, messageBody, cancellationToken);

        _logger.LogInformation($"Message published to queue {queueName}");
    }
}