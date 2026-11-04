namespace ClinicBooking.Domain.Exceptions;

public sealed class PastAppointmentException : DomainException
{
    public PastAppointmentException(DateTime requestedTimeUtc, DateTime currentUtc)
        : base($"Cannot book or schedule an appointment in the past: requested time {requestedTimeUtc:O} is earlier than current time {currentUtc:O}.")
    {
    }
}
