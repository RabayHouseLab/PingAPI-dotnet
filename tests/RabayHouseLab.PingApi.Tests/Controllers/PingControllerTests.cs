using FluentAssertions;
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

        // Assert — ActionResult<PingResponse> encapsula o resultado
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as PingResponse;
        response.Should().NotBeNull();
        response!.Message.Should().Be("pong");

        // Também valida via Value (outra forma de acesso)
        result.Value.Should().BeNull(); // Quando usa Ok(), Value é null e Result contém o OkObjectResult
    }

    [Fact]
    public void PingResponseShouldSerializeToCamelCaseJson()
    {
        // Arrange — valida contrato JSON esperado: { "message": "pong" }
        var response = new PingResponse("pong");

        // Act
        var json = JsonSerializer.Serialize(response, CachedCamelCaseOptions);

        // Assert
        json.Should().Be("""{"message":"pong"}""");
    }
}
