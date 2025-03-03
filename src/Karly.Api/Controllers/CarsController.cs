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

    [HttpPost(ApiEndpoints.Cars.Search)]
    public async Task<IActionResult> Search([FromBody] SearchCarCommand command,
        CancellationToken cancellationToken = default)
    {
        var carsDto = await _carService.SearchAsync(command.Input, cancellationToken);
        return Ok(carsDto);
    }

    [HttpPost(ApiEndpoints.Cars.Generate)]
    public async Task<IActionResult> Generate(CancellationToken cancellationToken = default)
    {
        await _carService.RegenerateAsync(cancellationToken);
        return Ok();
    }
}
