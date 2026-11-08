using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Commands.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTime NewStartTimeUtc,
    DateTime NewEndTimeUtc) : IRequest<Result<AppointmentDto>>;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("AppointmentId is required.");
        RuleFor(x => x.NewStartTimeUtc)
            .NotEmpty()
            .Must((cmd, start) => start < cmd.NewEndTimeUtc)
            .WithMessage("NewStartTimeUtc must be earlier than NewEndTimeUtc.");
    }
}

public sealed class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RescheduleAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AppointmentDto>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        var currentUtc = _dateTimeProvider.UtcNow;
        if (request.NewStartTimeUtc <= currentUtc)
        {
            return Result.Failure<AppointmentDto>(Error.Validation("Appointment.PastTime",
                "Cannot reschedule an appointment to a past time."));
        }

        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(appointment.DoctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Doctor", appointment.DoctorId));
        }

        var newRange = DateTimeRange.Create(request.NewStartTimeUtc, request.NewEndTimeUtc);

        // Check blackout dates
        if (doctor.IsBlockedAt(newRange))
        {
            return Result.Failure<AppointmentDto>(Error.Conflict("Doctor.BlackoutException",
                "The doctor has blocked availability during the requested new time."));
        }

        // Check conflicts
        var doctorHasConflict = await _appointmentRepository.HasConflictingAppointmentForDoctorAsync(
            appointment.TenantId,
            appointment.DoctorId,
            request.NewStartTimeUtc,
            request.NewEndTimeUtc,
            excludeAppointmentId: appointment.Id,
            cancellationToken);

        if (doctorHasConflict)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict("Appointment.DoctorSlotUnavailable",
                "The doctor already has an appointment during this new slot."));
        }

        var patientHasConflict = await _appointmentRepository.HasConflictingAppointmentForPatientAsync(
            appointment.TenantId,
            appointment.PatientId,
            request.NewStartTimeUtc,
            request.NewEndTimeUtc,
            excludeAppointmentId: appointment.Id,
            cancellationToken);

        if (patientHasConflict)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict("Appointment.PatientDoubleBooking",
                "The patient already has an overlapping appointment during this new slot."));
        }

        try
        {
            appointment.Reschedule(newRange, currentUtc);
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
        catch (PastAppointmentException pastEx)
        {
            return Result.Failure<AppointmentDto>(Error.Validation("Appointment.PastDate", pastEx.Message));
        }
        catch (InvalidAppointmentStateException stateEx)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict("Appointment.InvalidState", stateEx.Message));
        }
    }
}
