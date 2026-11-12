namespace ClinicBooking.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a patient waitlist entry.
/// </summary>
public enum WaitlistStatus
{
    Waiting = 1,
    Offered = 2,
    Booked = 3,
    Expired = 4,
    Cancelled = 5
}
