using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace ClinicBooking.Infrastructure.Tests.Authentication;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly FixedDateTimeProvider _dateTimeProvider;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "SuperSecretIntegrationTestingJwtKeyThatIsLongEnoughForSha256!",
            Issuer = "ClinicBookingTestIssuer",
            Audience = "ClinicBookingTestAudience",
            ExpiryMinutes = 30,
            RefreshTokenExpiryDays = 7
        };

        _dateTimeProvider = new FixedDateTimeProvider(new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc));
        _tokenService = new TokenService(Options.Create(_jwtSettings), _dateTimeProvider);
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidJwtWithExpectedClaims()
    {
        // Arrange
        var user = User.Create("doctor@clinic.com", "Sarah", "Connor", UserRole.Doctor, Guid.NewGuid());

        // Act
        var tokenString = _tokenService.GenerateAccessToken(user, "apex-health");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Equal(_jwtSettings.Issuer, jwt.Issuer);
        Assert.Contains(_jwtSettings.Audience, jwt.Audiences);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type is "sub" or ClaimTypes.NameIdentifier);
        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim.Value);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type is "role" or ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Doctor", roleClaim.Value);

        var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant");
        Assert.NotNull(tenantClaim);
        Assert.Equal("apex-health", tenantClaim.Value);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsOpaqueStringWithExpiration()
    {
        // Act
        var (token, jwtId, expiresAtUtc) = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(jwtId));
        Assert.Equal(_dateTimeProvider.UtcNow.AddDays(7), expiresAtUtc);
    }

    [Fact]
    public void ValidateExpiredToken_ExtractsUserIdFromExpiredToken()
    {
        // Arrange
        var user = User.Create("patient@clinic.com", "John", "Doe", UserRole.Patient, Guid.NewGuid());
        var tokenString = _tokenService.GenerateAccessToken(user, "apex-health");

        // Act (validates even when expired without failing)
        var extractedUserId = _tokenService.ValidateExpiredToken(tokenString);

        // Assert
        Assert.NotNull(extractedUserId);
        Assert.Equal(user.Id, extractedUserId.Value);
    }

    [Fact]
    public void ValidateExpiredToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        var user = User.Create("patient@clinic.com", "John", "Doe", UserRole.Patient, Guid.NewGuid());
        var tokenString = _tokenService.GenerateAccessToken(user, "apex-health");
        var tamperedToken = tokenString + "tampered";

        // Act
        var extractedUserId = _tokenService.ValidateExpiredToken(tamperedToken);

        // Assert
        Assert.Null(extractedUserId);
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; }
        public FixedDateTimeProvider(DateTime utcNow) => UtcNow = utcNow;
    }
}
