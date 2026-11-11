namespace ClinicBooking.Application.Features.Doctors.DTOs;

public sealed record DoctorShiftDto(
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record DoctorAvailabilityDto(
    Guid Id,
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive,
    string? DayOfWeekName = null,
    string? ClinicName = null,
    DoctorShiftDto? Shift = null);

public sealed record DoctorClinicDto(
    Guid ClinicId,
    string ClinicName,
    string ClinicCity,
    decimal ConsultationFee,
    bool IsPrimary = false);

public sealed record DoctorAvailabilityExceptionDto(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason,
    bool IsBlocked);

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
    IReadOnlyList<DoctorAvailabilityDto> Availabilities,
    string? BioAr = null,
    string? LocalizedBio = null,
    string? FullName = null,
    string? Email = null,
    string? SpecialtyName = null,
    IReadOnlyList<DoctorClinicDto>? DoctorClinics = null,
    IReadOnlyList<DoctorAvailabilityExceptionDto>? Exceptions = null);
