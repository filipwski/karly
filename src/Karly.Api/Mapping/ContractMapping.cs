using Karly.Application.Models;
using Karly.Contracts.Responses;

namespace Karly.Api.Mapping;

public static class ContractMapping
{
    public static GetCarDto MapToDto(this Car car)
    {
        return new GetCarDto
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Price = car.Price,
            ProductionYear = car.ProductionYear,
            Mileage = car.Mileage,
            IsNew = car.IsNew,
            IsElectric = car.IsElectric,
            HasAutomaticTransmission = car.HasAutomaticTransmission,
            Description = car.Description
        };
    }

    public static GetCarsDto MapToDto(this IEnumerable<Car> cars)
    {
        return new GetCarsDto { Items = cars.Select(MapToDto).ToList() };
    }
}