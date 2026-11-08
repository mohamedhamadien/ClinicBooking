namespace ClinicBooking.Application.Features.Tenants.DTOs;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Identifier,
    bool IsActive,
    int CancellationNoticeHours,
    bool RequiresDoctorApproval);
