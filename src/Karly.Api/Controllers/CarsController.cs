using System.Text.Json;
using Karly.Api.Services;
using Karly.Application.Mapping;
using Karly.Application.Services;
using Karly.Contracts.Commands;
using Karly.Contracts.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Karly.Api.Controllers;

[ApiController]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;
    private readonly RabbitMqPublisherService _rabbitMqPublisherService;
    public CarsController(ICarService carService, RabbitMqPublisherService rabbitMqPublisherService)
    {
        _carService = carService;
        _rabbitMqPublisherService = rabbitMqPublisherService;
    }

    [HttpGet(ApiEndpoints.Cars.Get)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var carDto = await _carService.GetAsync(id, cancellationToken);
        return carDto == null ? NotFound() : Ok(carDto);
    }
    
    [HttpGet(ApiEndpoints.Cars.GetAll)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var carsDto = await _carService.GetAllAsync(cancellationToken);
        return Ok(carsDto);
    }
    
    [HttpPost(ApiEndpoints.Cars.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var message = command.MapToCreateCarMessage();
        await _rabbitMqPublisherService.PublishCreateCarMessage(message, cancellationToken);
        return Accepted($"Message published for processing: {message.Make} {message.Model}");
    }

    [HttpGet(ApiEndpoints.Cars.Generate)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "ExampleCars.json");

        if (!System.IO.File.Exists(jsonFilePath))
            throw new FileNotFoundException($"Seed file not found: {jsonFilePath}");

        var jsonString = await System.IO.File.ReadAllTextAsync(jsonFilePath, cancellationToken);
        var carList = JsonSerializer.Deserialize<List<CreateCarMessage>>(jsonString);
        if (carList == null) return NotFound();

        var tasks = carList.Select(car => _rabbitMqPublisherService.PublishCreateCarMessage(car, cancellationToken));
        await Task.WhenAll(tasks);
        return Ok();
    }
}
