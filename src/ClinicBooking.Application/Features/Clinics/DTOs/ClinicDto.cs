namespace ClinicBooking.Application.Features.Clinics.DTOs;

public sealed record ClinicDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Address,
    string City,
    string PhoneNumber,
    bool IsActive);
