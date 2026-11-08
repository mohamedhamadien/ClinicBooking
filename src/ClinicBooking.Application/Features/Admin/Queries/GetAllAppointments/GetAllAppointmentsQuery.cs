using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Admin.Queries.GetAllAppointments;

public sealed record GetAllAppointmentsQuery(
    Guid TenantId,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? DoctorId = null) : IRequest<Result<IReadOnlyList<AppointmentDto>>>;

public sealed class GetAllAppointmentsQueryHandler
    : IRequestHandler<GetAllAppointmentsQuery, Result<IReadOnlyList<AppointmentDto>>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILocalizationService _localizationService;

    public GetAllAppointmentsQueryHandler(
        IAppointmentRepository appointmentRepository,
        ILocalizationService? localizationService = null)
    {
        _appointmentRepository = appointmentRepository;
        _localizationService = localizationService ?? new LocalizationService();
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> Handle(
        GetAllAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetAllAppointmentsAsync(
            request.TenantId,
            request.FromUtc,
            request.ToUtc,
            request.DoctorId,
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
