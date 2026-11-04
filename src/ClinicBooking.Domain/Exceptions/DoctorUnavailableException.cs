namespace ClinicBooking.Domain.Exceptions;

public sealed class DoctorUnavailableException : DomainException
{
    public DoctorUnavailableException(string reason) : base(reason)
    {
    }

    public DoctorUnavailableException(Guid doctorId, DateTime requestedStartUtc)
        : base($"Doctor {doctorId} is not available for booking at requested time {requestedStartUtc:O}.")
    {
    }
}
