using System.Text;
using System.Text.Json;
using Karly.Application.Options;
using Karly.Contracts.Messages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Karly.Api.Services;

public class RabbitMqPublisherService
{
    private readonly ILogger<RabbitMqPublisherService> _logger;
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisherService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisherService> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task PublishMessage<T>(T message, CancellationToken cancellationToken = default) where T : CreateCarMessage
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queue: _options.CreateCarQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        var addr = new PublicationAddress(exchangeType: ExchangeType.Direct, exchangeName: "", routingKey: _options.CreateCarQueueName);

        await channel.BasicPublishAsync(addr, props, messageBody, cancellationToken);
        
        _logger.LogInformation($"Message published to queue {_options.CreateCarQueueName}");
    }
}