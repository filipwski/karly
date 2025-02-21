using Karly.WorkerService.Services;

namespace Karly.WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started, initializing RabbitMQ consumer...");

        using var scope = _serviceScopeFactory.CreateScope();
        var rabbitMqConsumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumerService>();

        await rabbitMqConsumer.StartConsumingMessagesAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}