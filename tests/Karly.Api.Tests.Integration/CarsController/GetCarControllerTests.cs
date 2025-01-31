using System.Net;
using FluentAssertions;

namespace Karly.Api.Tests.Integration.CarsController;

[Collection(Consts.CommonCollectionDefinition)]
public class GetCarControllerTests
{
    private readonly HttpClient _client;

    public GetCarControllerTests(KarlyApiFactory apiFactory)
    {
        _client = apiFactory.CreateClient();
    }
    
    [Fact]
    public async Task Get_ReturnsNotFound_WhenCarsDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync(ApiEndpoints.Cars.Get.Replace("id:guid", "0194bd86-5d1f-722f-9dc3-662c99999999"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}