using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(
    Guid AppointmentId,
    string Reason,
    string CancelledByRole) : IRequest<Result<AppointmentDto>>;

public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("AppointmentId is required.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Cancellation reason is required.").MaximumLength(300);
        RuleFor(x => x.CancelledByRole).NotEmpty().Must(r => r is "Patient" or "Doctor" or "Admin")
            .WithMessage("CancelledByRole must be 'Patient', 'Doctor', or 'Admin'.");
    }
}

public sealed class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        ITenantRepository tenantRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _tenantRepository = tenantRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AppointmentDto>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        var currentUtc = _dateTimeProvider.UtcNow;

        try
        {
            if (request.CancelledByRole == "Patient")
            {
                var tenant = await _tenantRepository.GetByIdAsync(appointment.TenantId, cancellationToken);
                var noticeHours = tenant?.CancellationNoticeHours ?? 24;

                appointment.CancelByPatient(request.Reason, noticeHours, currentUtc);
            }
            else if (request.CancelledByRole == "Doctor")
            {
                appointment.CancelByDoctor(request.Reason, currentUtc);
            }
            else
            {
                appointment.CancelByAdmin(request.Reason, currentUtc);
            }

            await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        catch (AppointmentCancellationPolicyException policyEx)
        {
            return Result.Failure<AppointmentDto>(Error.Validation("Appointment.CancellationPolicyViolation", policyEx.Message));
        }
        catch (InvalidAppointmentStateException stateEx)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict("Appointment.InvalidState", stateEx.Message));
        }
    }
}
