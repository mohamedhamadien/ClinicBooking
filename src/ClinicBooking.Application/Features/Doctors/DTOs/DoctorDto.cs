namespace ClinicBooking.Application.Features.Doctors.DTOs;

public sealed record DoctorAvailabilityDto(
    Guid Id,
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public sealed record DoctorDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    Guid SpecialtyId,
    string LicenseNumber,
    string Bio,
    int DefaultSlotDurationMinutes,
    bool RequiresApproval,
    bool IsActive,
    IReadOnlyList<DoctorAvailabilityDto> Availabilities);
