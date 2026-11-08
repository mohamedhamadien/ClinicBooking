using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, string? tenantIdentifier);
    (string Token, string JwtId, DateTime ExpiresAtUtc) GenerateRefreshToken();
    Guid? ValidateExpiredToken(string token);
}
