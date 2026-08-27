using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PingApi.Api.Models;

namespace PingApi.Tests.Integration;

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

        // Assert — Status
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Assert — Body
        var body = await response.Content.ReadFromJsonAsync<PingResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("pong");
    }

    [Fact]
    public async Task GetHealthReturns200Async()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnknownRouteReturns404Async()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/unknown-route-xyz", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
