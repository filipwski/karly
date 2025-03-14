using Karly.Application.Models;
using Karly.Contracts.Commands;
using Karly.Contracts.Messages;
using Karly.Contracts.Responses;

namespace Karly.Application.Mapping;

public static class ContractMapping
{
    public static CarJsonModel MapToCarJsonModel(this Car car)
    {
        return new CarJsonModel
        {
            Make = car.Make,
            Model = car.Model,
            Price = car.Price,
            ProductionYear = car.ProductionYear,
            Mileage = car.Mileage,
            IsNew = car.IsNew,
            IsElectric = car.IsElectric,
            HasAutomaticTransmission = car.HasAutomaticTransmission,
            Description = car.Description,
            Embedding = car.CarEmbedding!.Embedding!.ToArray()
        };
    }
    
    public static CarEmbedding MapToCarEmbedding(this CreateCarEmbeddingCommand carEmbedding)
    {
        return new CarEmbedding
        {
            CarId = carEmbedding.CarId,
            Embedding = carEmbedding.Embedding,
        };
    }
    
    public static CreateCarMessage MapToCreateCarMessage(this CreateCarCommand car)
    {
        return new CreateCarMessage
        {
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
    
    public static CreateCarMessage MapToCreateCarMessage(this Car car)
    {
        return new CreateCarMessage
        {
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
    
    public static CreateCarCommand MapToCreateCarCommand(this CreateCarMessage car)
    {
        return new CreateCarCommand
        {
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
    
    public static Car MapToCar(this CreateCarCommand car)
    {
        return new Car
        {
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
    
    public static CarDto MapToDto(this Car car)
    {
        return new CarDto
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

    public static CarsDto MapToDto(this IEnumerable<Car> cars)
    {
        return new CarsDto { Items = cars.Select(MapToDto).ToList() };
    }
}