using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Queries.GetMyAppointments;

public sealed record GetMyAppointmentsQuery(Guid TenantId, Guid PatientId)
    : IRequest<Result<IReadOnlyList<AppointmentDto>>>;

public sealed class GetMyAppointmentsQueryHandler
    : IRequestHandler<GetMyAppointmentsQuery, Result<IReadOnlyList<AppointmentDto>>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILocalizationService _localizationService;

    public GetMyAppointmentsQueryHandler(
        IAppointmentRepository appointmentRepository,
        ILocalizationService? localizationService = null)
    {
        _appointmentRepository = appointmentRepository;
        _localizationService = localizationService ?? new LocalizationService();
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> Handle(
        GetMyAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetMyAppointmentsAsync(
            request.TenantId,
            request.PatientId,
            cancellationToken);

        var dtos = appointments.Select(a => new AppointmentDto(
            a.Id,
            a.TenantId,
            a.DoctorId,
            a.PatientId,
            a.ClinicId,
            a.ScheduledRange.StartUtc,
            a.ScheduledRange.EndUtc,
            a.Status,
            a.Notes,
            a.CancellationReason,
            a.CancelledAtUtc,
            _localizationService.GetStatusName(a.Status))).ToList();

        return Result.Success<IReadOnlyList<AppointmentDto>>(dtos);
    }
}
