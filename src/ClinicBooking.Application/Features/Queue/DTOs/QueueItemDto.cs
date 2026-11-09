using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Features.Queue.DTOs;

public sealed record QueueItemDto(
    Guid AppointmentId,
    int? QueueNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    Guid ClinicId,
    string ClinicName,
    DateTime StartTimeUtc,
    AppointmentStatus Status);
