using System.Text;
using System.Text.Json;
using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Application.Options;
using Karly.Application.Services;
using Karly.Contracts.Commands;
using Karly.Contracts.Messages;
using Microsoft.Extensions.Options;
using Pgvector;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Karly.WorkerService.Services;

public class RabbitMqConsumerService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConsumerService> logger, IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task StartConsumingMessagesAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await _channel.ExchangeDeclareAsync(
            exchange: "car_creation_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );
        
        await _channel.QueueDeclareAsync(
            queue: _options.CreateCarQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx_exchange" },
                { "x-dead-letter-routing-key", "dead_letter_routing_key" }
            }!,
            cancellationToken: cancellationToken
        );
        
        await _channel.QueueBindAsync(
            queue: _options.CreateCarQueueName,
            exchange: "car_creation_exchange",
            routingKey: "car_creation_routing_key",
            cancellationToken: cancellationToken
        );
        
        await _channel.ExchangeDeclareAsync(
            exchange: "retry_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );
        
        await _channel.QueueDeclareAsync(
            queue: "retry_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "car_creation_exchange" },
                { "x-dead-letter-routing-key", "car_creation_routing_key" },
                { "x-message-ttl", 10000 }
            }!,
            cancellationToken: cancellationToken
        );
        
        await _channel.QueueBindAsync(
            queue: "retry_queue",
            exchange: "retry_exchange",
            routingKey: "retry_routing_key",
            cancellationToken: cancellationToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: "dlx_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: "dead_letter_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: "dead_letter_queue",
            exchange: "dlx_exchange",
            routingKey: "dead_letter_routing_key",
            cancellationToken: cancellationToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                _logger.LogInformation($"Received message: {message}");

                var carData = JsonSerializer.Deserialize<CreateCarMessage>(message);
                if (carData != null)
                {
                    await ProcessCarCreationAsync(carData, cancellationToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                var retryCount = ea.Redelivered ? 1 : 0;

                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.TryGetValue("x-delivery-count", out var countObj))
                {
                    retryCount = Convert.ToInt32(countObj) + 1;
                }

                _logger.LogError($"Error processing message. Retry count: {retryCount}. Error: {ex.Message}");

                if (retryCount < 3)
                {
                    var updatedHeaders = ea.BasicProperties.Headers != null
                        ? new Dictionary<string, object>(ea.BasicProperties.Headers!)
                        : new Dictionary<string, object>();

                    updatedHeaders["x-delivery-count"] = retryCount;

                    var properties = new BasicProperties
                    {
                        Headers = updatedHeaders!
                    };

                    await _channel.BasicPublishAsync(
                        exchange: "retry_exchange",
                        routingKey: "retry_routing_key",
                        mandatory: true,
                        basicProperties: properties,
                        body: ea.Body,
                        cancellationToken
                    );

                    _logger.LogWarning($"Message requeued for retry {retryCount}/3");
                }
                else
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                }
            }
        };
        
        await _channel.BasicConsumeAsync(queue: _options.CreateCarQueueName, autoAck: false, consumer: consumer, cancellationToken);

        await Task.Delay(1000, cancellationToken);
    }

    private async Task ProcessCarCreationAsync(CreateCarMessage message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KarlyDbContext>();
        var carService = scope.ServiceProvider.GetRequiredService<ICarService>();
        var carEmbeddingService = scope.ServiceProvider.GetRequiredService<ICarEmbeddingService>();

        _logger.LogInformation($"Processing car: {message.Make} {message.Model}");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var createdCar = await carService.Create(message.MapToCreateCarCommand(), cancellationToken);
        if (createdCar == null)
        {
            throw new Exception("Failed to create car in the database.");
        }

        _logger.LogInformation($"Car created with ID: {createdCar.Id}");

        var embeddings = await carEmbeddingService.GenerateEmbeddingsAsync(createdCar, cancellationToken);

        if (embeddings == null || embeddings.Count == 0)
        {
            throw new Exception("Failed to generate embeddings from OpenAI.");
        }

        _logger.LogInformation("Generated embedding successfully.");

        var createEmbeddingCommand = new CreateCarEmbeddingCommand
        {
            CarId = createdCar.Id,
            Embedding = new Vector(embeddings[0].ToArray())
        };

        await carEmbeddingService.CreateCarEmbeddingEntityAsync(createEmbeddingCommand, cancellationToken);

        _logger.LogInformation("Embedding saved to the database.");

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Car creation process completed successfully.");
    }
}