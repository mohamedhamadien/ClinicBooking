namespace ClinicBooking.Application.Features.Staff.DTOs;

public sealed record StaffMemberDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role,
    bool IsActive,
    Guid? ClinicId,
    string? ClinicName,
    IReadOnlyList<string> Permissions,
    DateTime CreatedAtUtc);

public sealed record PermissionItemDto(
    string Key,
    string NameEn,
    string NameAr,
    string Category,
    string Description);

public sealed record PermissionCatalogDto(
    IReadOnlyList<PermissionItemDto> Permissions,
    IReadOnlyDictionary<string, IReadOnlyList<string>> RolePresets);
