using System.Net;
using System.Net.Http.Json;
using ClinicBooking.Application.Features.Authentication.Commands.Login;
using ClinicBooking.Application.Features.Authentication.Commands.RefreshToken;
using ClinicBooking.Application.Features.Authentication.Commands.RegisterPatient;
using ClinicBooking.Application.Features.Authentication.DTOs;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Tests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidPatientData_ReturnsOkAndTokens()
    {
        // Arrange
        var command = new RegisterPatientCommand(
            _factory.DefaultTenantId,
            $"patient_{Guid.NewGuid():N}@clinic.com",
            "SecureP@ssw0rd123!",
            "Ahmed",
            "Ali",
            new DateOnly(1992, 5, 15),
            Gender.Male,
            "+201011112222");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authResponse.RefreshToken));
        Assert.Equal("Ahmed", authResponse.User.FirstName);
        Assert.Equal("Patient", authResponse.User.Role);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequestProblemDetails()
    {
        // Arrange
        var command = new RegisterPatientCommand(
            _factory.DefaultTenantId,
            $"patient_{Guid.NewGuid():N}@clinic.com",
            "weak", // violates password length and character rules
            "Jane",
            "Doe",
            new DateOnly(1995, 1, 1),
            Gender.Female);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Title is "Validation Failed" or "Bad Request");
    }

    [Fact]
    public async Task Login_WithAdminCredentials_ReturnsOkAndJwtToken()
    {
        // Arrange
        var login = new LoginCommand("admin@clinicbooking.com", "Admin@Secure123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", login);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.Equal("Admin", authResponse.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var login = new LoginCommand("admin@clinicbooking.com", "WrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", login);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesAndReturnsNewTokens()
    {
        // Arrange: first login to get valid tokens
        var login = new LoginCommand("admin@clinicbooking.com", "Admin@Secure123!");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", login);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(loginData);

        var refreshCommand = new RefreshTokenCommand(loginData.AccessToken, loginData.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshCommand);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshData = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(refreshData);
        Assert.False(string.IsNullOrWhiteSpace(refreshData.AccessToken));
        Assert.NotEqual(loginData.RefreshToken, refreshData.RefreshToken); // Token rotated
    }
}
