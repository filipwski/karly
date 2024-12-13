using Karly.Api.Mapping;
using Karly.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Karly.Api.Controllers;

[ApiController]
public class CarsController(ICarService carService) : ControllerBase
{
    [HttpGet(ApiEndpoints.Cars.Get)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var car = await carService.GetAsync(id);
        return car == null ? NotFound() : Ok(car.MapToDto());
    }
    
    [HttpGet(ApiEndpoints.Cars.GetAll)]
    public async Task<IActionResult> GetAll()
    {
        var cars = await carService.GetAllAsync();
        return Ok(cars.MapToDto());
    }
}
