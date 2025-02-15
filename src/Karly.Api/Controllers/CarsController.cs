using Karly.Api.Mapping;
using Karly.Api.Services;
using Karly.Contracts.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Karly.Api.Controllers;

[ApiController]
public class CarsController(ICarService carService) : ControllerBase
{
    [HttpGet(ApiEndpoints.Cars.Get)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var car = await carService.GetAsync(id, cancellationToken);
        return car == null ? NotFound() : Ok(car.MapToDto());
    }
    
    [HttpGet(ApiEndpoints.Cars.GetAll)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var cars = await carService.GetAllAsync(cancellationToken);
        return Ok(cars.MapToDto());
    }
    
    [HttpPost(ApiEndpoints.Cars.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCarCommand command, CancellationToken cancellationToken = default)
    {
        var carDto = await carService.Create(command, cancellationToken);
        return Ok(carDto);
    }
}
