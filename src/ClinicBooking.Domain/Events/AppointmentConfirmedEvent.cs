using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Events;

public sealed record AppointmentConfirmedEvent(
    Guid AppointmentId,
    Guid DoctorId,
    Guid PatientId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
