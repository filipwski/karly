using Karly.Api.Services;
using Karly.Application.Mapping;
using Karly.Application.Services;
using Karly.Contracts.Commands;
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
        var car = await _carService.GetAsync(id, cancellationToken);
        return car == null ? NotFound() : Ok(car.MapToDto());
    }
    
    [HttpGet(ApiEndpoints.Cars.GetAll)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var cars = await _carService.GetAllAsync(cancellationToken);
        return Ok(cars.MapToDto());
    }
    
    [HttpPost(ApiEndpoints.Cars.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var message = command.MapToCreateCarMessage();
        await _rabbitMqPublisherService.PublishMessage(message, cancellationToken);
        return Accepted($"Message published for processing: {message.Make} {message.Model}");
    }
}
