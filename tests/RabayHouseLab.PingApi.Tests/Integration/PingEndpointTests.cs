using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RabayHouseLab.PingApi.Api.Models;

namespace RabayHouseLab.PingApi.Tests.Integration;

public sealed class PingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PingEndpointTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPingReturns200WithPongMessageAsync()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/ping", UriKind.Relative));

        // Assert - Status
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // Assert - Body
        var body = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(body);
        Assert.Equal("pong", body!.Message);
    }

    [Fact]
    public async Task GetHealthReturns200Async()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUnknownRouteReturns404Async()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/unknown-route-xyz", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
