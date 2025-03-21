using Karly.Application.Mapping;
using Karly.Application.Services;
using Karly.Contracts.Commands;
using Karly.Contracts.Responses;
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
        return Accepted();
    }

    [HttpPost(ApiEndpoints.Cars.Search)]
    public async Task<IActionResult> Search([FromBody] SearchCarCommand command,
        CancellationToken cancellationToken = default)
    {
        var carsDto = await _carService.SearchAsync(command.Input, cancellationToken);
        return Ok(carsDto);
    }

    [HttpPatch(ApiEndpoints.Cars.GenerateDescription)]
    public async Task<IActionResult> GenerateDescription([FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var carDto = await _carService.GenerateAndUpdateDescriptionAsync(id, cancellationToken);
        if (carDto == null) return NotFound();

        var carsDto = new CarsDto { Items = new List<CarDto> { carDto } };
        await _rabbitMqPublisherService.PublishRegenerateCarEmbeddingsMessage(
            carsDto.MapToRegenerateCarEmbeddingsMessage(), cancellationToken);

        return Ok(carDto);
    }

    [HttpPost(ApiEndpoints.Cars.Regenerate)]
    public async Task<IActionResult> Regenerate(CancellationToken cancellationToken = default)
    {
        var cars = await _carService.GetAllAsync(cancellationToken);
        var message = cars.MapToRegenerateCarEmbeddingsMessage();
        await _rabbitMqPublisherService.PublishRegenerateCarEmbeddingsMessage(message, cancellationToken);
        return Accepted();
    }
}
