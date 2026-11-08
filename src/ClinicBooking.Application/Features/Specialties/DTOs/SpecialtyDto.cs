namespace ClinicBooking.Application.Features.Specialties.DTOs;

public sealed record SpecialtyDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string Description,
    bool IsActive,
    string? NameAr = null,
    string? DescriptionAr = null,
    string? LocalizedName = null,
    string? LocalizedDescription = null);
