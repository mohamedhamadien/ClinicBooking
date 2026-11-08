using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Appointments.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed record GetAppointmentByIdQuery(Guid AppointmentId) : IRequest<Result<AppointmentDto>>;

public sealed class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, Result<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public GetAppointmentByIdQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<Result<AppointmentDto>> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        var dto = new AppointmentDto(
            appointment.Id,
            appointment.TenantId,
            appointment.DoctorId,
            appointment.PatientId,
            appointment.ClinicId,
            appointment.ScheduledRange.StartUtc,
            appointment.ScheduledRange.EndUtc,
            appointment.Status,
            appointment.Notes,
            appointment.CancellationReason,
            appointment.CancelledAtUtc);

        return Result.Success(dto);
    }
}
