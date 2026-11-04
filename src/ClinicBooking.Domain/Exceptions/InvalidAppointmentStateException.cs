using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Exceptions;

public sealed class InvalidAppointmentStateException : DomainException
{
    public InvalidAppointmentStateException(AppointmentStatus currentStatus, string targetAction)
        : base($"Cannot execute action '{targetAction}' when appointment is in state '{currentStatus}'.")
    {
    }
}
