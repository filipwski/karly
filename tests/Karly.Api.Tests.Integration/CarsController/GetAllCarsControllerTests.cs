using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Karly.Contracts.Responses;

namespace Karly.Api.Tests.Integration.CarsController;

[Collection(Consts.CommonCollectionDefinition)]
public class GetAllCarsControllerTests
{
    private readonly HttpClient _client;

    public GetAllCarsControllerTests(KarlyApiFactory apiFactory)
    {
        _client = apiFactory.CreateClient();
    }
    
    [Fact]
    public async Task GetAll_ReturnsAllCars_WhenCarsExist()
    {
        // Act
        var response = await _client.GetAsync(ApiEndpoints.Cars.GetAll);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getCarsDto = await response.Content.ReadFromJsonAsync<CarsDto>();
        getCarsDto!.Items.Should().NotBeEmpty();
    }
}