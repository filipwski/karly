using Karly.Api.Mapping;
using Karly.Application.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karly.Api.Controllers;

[ApiController]
public class CarsController(KarlyDbContext dbContext) : ControllerBase
{
    [HttpGet(ApiEndpoints.Cars.Get)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var car = await dbContext.Cars.FindAsync(id);
        return car == null ? NotFound() : Ok(car.MapToDto());
    }
    
    [HttpGet(ApiEndpoints.Cars.GetAll)]
    public async Task<IActionResult> GetAll()
    {
        var cars = await dbContext.Cars.ToListAsync();
        return Ok(cars.MapToDto());
    }
}
