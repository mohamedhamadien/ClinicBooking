using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Events;

public sealed record AppointmentRescheduledEvent(
    Guid AppointmentId,
    Guid DoctorId,
    Guid PatientId,
    DateTime OldStartUtc,
    DateTime NewStartUtc,
    DateTime NewEndUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
