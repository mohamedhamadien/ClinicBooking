using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Events;

public sealed record AppointmentCancelledEvent(
    Guid AppointmentId,
    Guid DoctorId,
    Guid PatientId,
    string CancelledBy,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
