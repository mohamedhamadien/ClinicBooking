namespace ClinicBooking.Domain.Exceptions;

public sealed class SlotDurationInvalidException : DomainException
{
    public SlotDurationInvalidException(int minutes)
        : base($"Slot duration {minutes} minutes is invalid. Allowed duration must be between 10 and 180 minutes.")
    {
    }
}
