namespace ClinicBooking.Application.Features.Appointments.DTOs;

public sealed record AvailableSlotDto(
    Guid DoctorId,
    Guid ClinicId,
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes,
    string? DayOfWeekName = null,
    bool IsAvailable = true)
{
    public DateTime StartTimeUtc => StartUtc;
    public DateTime EndTimeUtc => EndUtc;
    public string StartLocal => StartUtc.ToString("HH:mm");
    public string EndLocal => EndUtc.ToString("HH:mm");
}
