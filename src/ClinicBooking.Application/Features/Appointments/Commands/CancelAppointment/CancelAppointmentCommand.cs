using ClinicBooking.Application.Common.Localization;
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
        RuleFor(x => x.CancelledByRole).NotEmpty().Must(r => r is "Patient" or "Doctor" or "Admin" or "ClinicAdmin" or "Receptionist" or "Staff")
            .WithMessage("CancelledByRole must be 'Patient', 'Doctor', 'Admin', 'ClinicAdmin', 'Receptionist', or 'Staff'.");
    }
}

public sealed class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWaitlistRepository? _waitlistRepository;

    public CancelAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        ITenantRepository tenantRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILocalizationService? localizationService = null,
        IWaitlistRepository? waitlistRepository = null)
    {
        _appointmentRepository = appointmentRepository;
        _tenantRepository = tenantRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _localizationService = localizationService ?? new LocalizationService();
        _waitlistRepository = waitlistRepository;
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
            else if (request.CancelledByRole is "Admin" or "ClinicAdmin" or "Receptionist" or "Staff")
            {
                appointment.CancelByAdmin(request.Reason, currentUtc);
            }
        }
        catch (AppointmentCancellationPolicyException)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict(
                "Appointment.CancellationPolicyViolation",
                _localizationService.GetString(LocalizationKeys.AppointmentCancellationNoticeExpired)));
        }
        catch (InvalidAppointmentStateException)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict(
                "Appointment.InvalidState",
                _localizationService.GetString(LocalizationKeys.AppointmentInvalidStatusTransition)));
        }

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        // Automatic Cancellation Recovery: offer the freed slot to the highest-priority waiting patient
        if (_waitlistRepository is not null)
        {
            var slotDate = DateOnly.FromDateTime(appointment.ScheduledRange.StartUtc);
            var slotTime = TimeOnly.FromDateTime(appointment.ScheduledRange.StartUtc);
            var nextWaiting = await _waitlistRepository.GetNextWaitingCandidateAsync(
                appointment.TenantId,
                appointment.ClinicId,
                appointment.DoctorId,
                slotDate,
                slotTime,
                cancellationToken);

            if (nextWaiting is not null)
            {
                nextWaiting.OfferSlot(
                    appointment.ScheduledRange.StartUtc,
                    appointment.ScheduledRange.EndUtc,
                    currentUtc.AddMinutes(30));
                await _waitlistRepository.UpdateAsync(nextWaiting, cancellationToken);
            }
        }

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
            appointment.CancelledAtUtc,
            _localizationService.GetStatusName(appointment.Status));

        return Result.Success(dto);
    }
}
