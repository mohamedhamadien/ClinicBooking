using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Events;

public sealed record AppointmentRejectedEvent(
    Guid AppointmentId,
    Guid DoctorId,
    Guid PatientId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
