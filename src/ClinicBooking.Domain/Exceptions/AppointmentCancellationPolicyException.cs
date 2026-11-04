namespace ClinicBooking.Domain.Exceptions;

public sealed class AppointmentCancellationPolicyException : DomainException
{
    public AppointmentCancellationPolicyException(int requiredNoticeHours, double actualNoticeHours)
        : base($"Cancellation policy violated: Patient cancellations require at least {requiredNoticeHours} hours advance notice. Current notice is {actualNoticeHours:F1} hours.")
    {
    }
}
