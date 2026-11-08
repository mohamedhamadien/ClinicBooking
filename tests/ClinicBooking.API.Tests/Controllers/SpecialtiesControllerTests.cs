using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicBooking.Application.Features.Authentication.Commands.Login;
using ClinicBooking.Application.Features.Authentication.DTOs;
using ClinicBooking.Application.Features.Specialties.Commands.CreateSpecialty;
using ClinicBooking.Application.Features.Specialties.DTOs;

namespace ClinicBooking.API.Tests.Controllers;

public class SpecialtiesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SpecialtiesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSpecialties_ReturnsOkWithBilingualSpecialties()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/specialties");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var specialties = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpecialtyDto>>();
        Assert.NotNull(specialties);
        Assert.NotEmpty(specialties);

        var cardiology = specialties.FirstOrDefault(s => s.Name == "Cardiology");
        Assert.NotNull(cardiology);
        Assert.Equal("أمراض القلب", cardiology.NameAr);
    }

    [Fact]
    public async Task CreateSpecialty_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var command = new CreateSpecialtyCommand("Neurology", "Brain care");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/specialties", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSpecialty_WithAdminToken_ReturnsOkAndCreatesSpecialty()
    {
        // Arrange: Login as Admin
        var login = new LoginCommand("admin@clinicbooking.com", "Admin@Secure123!");
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", login);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        var command = new CreateSpecialtyCommand(
            Name: "Pulmonology",
            Description: "Respiratory system and lung conditions.",
            NameAr: "أمراض الصدر والجهاز التنفسي",
            DescriptionAr: "تشخيص وعلاج أمراض الرئتين والجهاز التنفسي.");

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/specialties")
        {
            Content = JsonContent.Create(command)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await _client.SendAsync(request);
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var specialty = await response.Content.ReadFromJsonAsync<SpecialtyDto>();
        Assert.NotNull(specialty);
        Assert.Equal("Pulmonology", specialty.Name);
        Assert.Equal("أمراض الصدر والجهاز التنفسي", specialty.NameAr);
    }
}
