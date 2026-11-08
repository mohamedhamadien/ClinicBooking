namespace ClinicBooking.Application.Features.Specialties.DTOs;

public sealed record SpecialtyDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string Description,
    bool IsActive);
