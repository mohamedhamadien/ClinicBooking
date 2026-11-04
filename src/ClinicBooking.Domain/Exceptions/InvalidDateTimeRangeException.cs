namespace ClinicBooking.Domain.Exceptions;

public sealed class InvalidDateTimeRangeException : DomainException
{
    public InvalidDateTimeRangeException(DateTime start, DateTime end)
        : base($"Invalid date time range: Start ({start:O}) must be strictly earlier than End ({end:O}).")
    {
    }

    public InvalidDateTimeRangeException(string message) : base(message)
    {
    }
}
