namespace ClinicBooking.Application.Features.Admin.DTOs;

public sealed record DashboardStatsDto(
    int TotalAppointments,
    int ScheduledAppointments,
    int ConfirmedAppointments,
    int CompletedAppointments,
    int CancelledAppointments,
    int TotalDoctors,
    int TotalClinics);
