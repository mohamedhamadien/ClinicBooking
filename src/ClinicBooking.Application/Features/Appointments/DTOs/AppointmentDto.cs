using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Features.Appointments.DTOs;

public sealed record AppointmentDto(
    Guid Id,
    Guid TenantId,
    Guid DoctorId,
    Guid PatientId,
    Guid ClinicId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    AppointmentStatus Status,
    string? Notes,
    string? CancellationReason,
    DateTime? CancelledAtUtc,
    string? StatusDescription = null,
    string? DoctorName = null,
    string? PatientName = null,
    string? ClinicName = null,
    string? PatientPhone = null);
