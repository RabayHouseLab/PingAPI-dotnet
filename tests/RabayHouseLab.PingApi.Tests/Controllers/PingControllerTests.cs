using Microsoft.AspNetCore.Mvc;
using RabayHouseLab.PingApi.Api.Controllers;
using RabayHouseLab.PingApi.Api.Models;
using System.Text.Json;

namespace RabayHouseLab.PingApi.Tests.Controllers;

public sealed class PingControllerTests
{
    private static readonly JsonSerializerOptions CachedCamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void GetReturnsOkWithPongMessage()
    {
        // Arrange
        var controller = new PingController();

        // Act
        var result = controller.Get();

        // Assert - ActionResult<PingResponse> encapsula o resultado
        Assert.IsType<OkObjectResult>(result.Result);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        var response = Assert.IsType<PingResponse>(okResult.Value);
        Assert.Equal("pong", response.Message);

        // Tambem valida via Value (outra forma de acesso)
        Assert.Null(result.Value); // Quando usa Ok(), Value e null e Result contem o OkObjectResult
    }

    [Fact]
    public void PingResponseShouldSerializeToCamelCaseJson()
    {
        // Arrange - valida contrato JSON esperado: { "message": "pong" }
        var response = new PingResponse("pong");

        // Act
        var json = JsonSerializer.Serialize(response, CachedCamelCaseOptions);

        // Assert
        Assert.Equal("""{"message":"pong"}""", json);
    }
}
