namespace ClinicBooking.Application.Features.Appointments.DTOs;

public sealed record AvailableSlotDto(
    Guid DoctorId,
    Guid ClinicId,
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes);
