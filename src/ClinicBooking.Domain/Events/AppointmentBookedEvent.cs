using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Events;

public sealed record AppointmentBookedEvent(
    Guid AppointmentId,
    Guid DoctorId,
    Guid PatientId,
    Guid ClinicId,
    DateTime StartUtc,
    DateTime EndUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
