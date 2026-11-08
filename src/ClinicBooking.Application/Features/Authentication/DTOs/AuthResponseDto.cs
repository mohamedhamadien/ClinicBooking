namespace ClinicBooking.Application.Features.Authentication.DTOs;

public sealed record UserDto(
    Guid Id,
    Guid? TenantId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive);

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserDto User);
