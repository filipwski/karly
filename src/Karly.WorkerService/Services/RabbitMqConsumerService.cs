using System.Text;
using System.Text.Json;
using Karly.Application.Database;
using Karly.Application.Mapping;
using Karly.Application.Options;
using Karly.Application.Services;
using Karly.Contracts.Commands;
using Karly.Contracts.Messages;
using Microsoft.EntityFrameworkCore;
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

    public RabbitMqConsumerService(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConsumerService> logger,
        IServiceScopeFactory scopeFactory)
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
            exchange: "create_car_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: "regenerate_car_embeddings_exchange",
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
                { "x-dead-letter-routing-key", $"{_options.CreateCarQueueName}.dlx" }
            }!,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: _options.RegenerateCarEmbeddingsQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx_exchange" },
                { "x-dead-letter-routing-key", $"{_options.RegenerateCarEmbeddingsQueueName}.dlx" }
            }!,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _options.CreateCarQueueName,
            exchange: "create_car_exchange",
            routingKey: "create_car_routing_key",
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _options.RegenerateCarEmbeddingsQueueName,
            exchange: "regenerate_car_embeddings_exchange",
            routingKey: "regenerate_car_embeddings_routing_key",
            cancellationToken: cancellationToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: "create_car_retry_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: "create_car_retry_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "create_car_exchange" },
                { "x-dead-letter-routing-key", "create_car_routing_key" },
                { "x-message-ttl", 10000 }
            }!,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: "create_car_retry_queue",
            exchange: "create_car_retry_exchange",
            routingKey: "create_car_retry_routing_key",
            cancellationToken: cancellationToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: "regenerate_car_embeddings_retry_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: "regenerate_car_embeddings_retry_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "create_car_exchange" },
                { "x-dead-letter-routing-key", "create_car_routing_key" },
                { "x-message-ttl", 10000 }
            }!,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: "regenerate_car_embeddings_retry_queue",
            exchange: "regenerate_car_embeddings_retry_exchange",
            routingKey: "regenerate_car_embeddings_retry_routing_key",
            cancellationToken: cancellationToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: "dlx_exchange",
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: "create_car_dead_letter_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: "create_car_dead_letter_queue",
            exchange: "dlx_exchange",
            routingKey: $"{_options.CreateCarQueueName}.dlx",
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: "regenerate_car_embeddings_dead_letter_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: "regenerate_car_embeddings_dead_letter_queue",
            exchange: "dlx_exchange",
            routingKey: $"{_options.RegenerateCarEmbeddingsQueueName}.dlx",
            cancellationToken: cancellationToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation($"Received message: {TruncateString(message)}");

            using var document = JsonDocument.Parse(message);
            if (!document.RootElement.TryGetProperty("Type", out JsonElement typeElement))
            {
                _logger.LogWarning("Received message without a Type field. Message: {Message}", message);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                return;
            }

            var messageType = typeElement.GetString();

            if (messageType == nameof(CreateCarMessage))
            {
                try
                {
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

                    if (ea.BasicProperties.Headers != null &&
                        ea.BasicProperties.Headers.TryGetValue("x-delivery-count", out var countObj))
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
                            exchange: "create_car_retry_exchange",
                            routingKey: "create_car_retry_routing_key",
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
            }
            else if (messageType == nameof(RegenerateCarEmbeddingsMessage))
            {
                try
                {
                    var carData = JsonSerializer.Deserialize<RegenerateCarEmbeddingsMessage>(message);
                    if (carData != null)
                    {
                        await ProcessRecreationOfEmbeddingsAsync(carData, cancellationToken);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    var retryCount = ea.Redelivered ? 1 : 0;

                    if (ea.BasicProperties.Headers != null &&
                        ea.BasicProperties.Headers.TryGetValue("x-delivery-count", out var countObj))
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
                            exchange: "regenerate_car_embeddings_retry_exchange",
                            routingKey: "regenerate_car_embeddings_retry_routing_key",
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
            }
        };

        await _channel.BasicConsumeAsync(queue: _options.CreateCarQueueName, autoAck: false, consumer: consumer, cancellationToken);
        await _channel.BasicConsumeAsync(queue: _options.RegenerateCarEmbeddingsQueueName, autoAck: false, consumer: consumer, cancellationToken);

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

        var embedding = await carEmbeddingService.GenerateEmbeddingAsync(createdCar, cancellationToken);

        if (embedding == null)
        {
            throw new Exception("Failed to generate embeddings from OpenAI.");
        }

        _logger.LogInformation("Generated embedding successfully.");

        var createEmbeddingCommand = new CreateCarEmbeddingCommand
        {
            CarId = createdCar.Id,
            Embedding = new Vector((ReadOnlyMemory<float>)embedding)
        };

        await carEmbeddingService.CreateCarEmbeddingEntityAsync(createEmbeddingCommand, cancellationToken);

        _logger.LogInformation("Embedding saved to the database.");

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Car creation process completed successfully.");
    }

    private async Task ProcessRecreationOfEmbeddingsAsync(RegenerateCarEmbeddingsMessage message, CancellationToken cancellationToken)
    {
        var carsDto = message.CarsDto;
        
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KarlyDbContext>();
        var carEmbeddingService = scope.ServiceProvider.GetRequiredService<ICarEmbeddingService>();
        
        var carIdAndEmbeddings = await carEmbeddingService.GenerateEmbeddingsAsync(carsDto, cancellationToken);

        foreach (var carIdAndEmbedding in carIdAndEmbeddings!)
        {
            var carEmbedding = dbContext.CarEmbeddings.Single(embedding => embedding.CarId == carIdAndEmbedding.Key);
            
            var newEmbedding = new Vector(carIdAndEmbedding.Value);

            if (carEmbedding.Embedding.Equals(newEmbedding))
            {
                _logger.LogInformation($"Skipping update for CarId {carEmbedding.CarId}, embedding is identical.");
                continue;
            }

            _logger.LogInformation($"Updating embedding for CarId {carEmbedding.CarId}");
            carEmbedding.Embedding = newEmbedding;
            dbContext.Entry(carEmbedding).State = EntityState.Modified;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation($"Car embedding regeneration process completed successfully. Regenerated {carIdAndEmbeddings.Count} embedding records.");

        await ExportCarsToJsonAsync(dbContext, cancellationToken);
    }
    
    private async Task ExportCarsToJsonAsync(KarlyDbContext dbContext, CancellationToken cancellationToken)
    {
        var carsJsonModel = await dbContext.Cars
            .Include(c => c.CarEmbedding)
            .Select(c => c.MapToCarJsonModel())
            .ToListAsync(cancellationToken);
        
        var jsonString = JsonSerializer.Serialize(carsJsonModel, new JsonSerializerOptions
        {
            WriteIndented = true 
        });
    
        var projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        var filePath = Path.Combine(projectRoot, "Karly.Application", "Database", "Resources", "ExampleCars.json");
        
        _logger.LogInformation($"Writing JSON to file: {filePath}");
        
        await File.WriteAllTextAsync(filePath, jsonString, cancellationToken);
    
        _logger.LogInformation("ExampleCars.json has been overriden successfully.");
    }

    private string TruncateString(string input, int length = 200)
    {
        return input.Length > length ? input.Substring(0, length) + "..." : input;
    }
}