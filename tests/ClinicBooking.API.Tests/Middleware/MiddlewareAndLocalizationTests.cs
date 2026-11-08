using System.Net;
using System.Net.Http.Json;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Tests.Middleware;

public class MiddlewareAndLocalizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MiddlewareAndLocalizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CorrelationId_AlwaysPropagatedToResponseHeaders()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var correlationId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    [Fact]
    public async Task CorrelationId_CustomClientCorrelationId_PreservedInResponse()
    {
        // Arrange
        var customId = "Client-Trace-" + Guid.NewGuid().ToString("N");
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", customId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var returnedId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        Assert.Equal(customId, returnedId);
    }

    [Fact]
    public async Task HealthLive_ReturnsOkWithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task SwaggerEndpoint_ReturnsOkAndValidJson()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Clinic Appointment Booking Web API", json);
        Assert.Contains("X-Tenant-Slug", json);
    }
}
