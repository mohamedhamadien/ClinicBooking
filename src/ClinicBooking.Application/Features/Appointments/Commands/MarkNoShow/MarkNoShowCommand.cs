using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Commands.MarkNoShow;

public sealed record MarkNoShowCommand(Guid AppointmentId) : IRequest<Result<AppointmentDto>>;

public sealed class MarkNoShowCommandValidator : AbstractValidator<MarkNoShowCommand>
{
    public MarkNoShowCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("AppointmentId is required.");
    }
}

public sealed class MarkNoShowCommandHandler : IRequestHandler<MarkNoShowCommand, Result<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNoShowCommandHandler(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        ILocalizationService? localizationService = null)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _localizationService = localizationService ?? new LocalizationService();
    }

    public async Task<Result<AppointmentDto>> Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        try
        {
            appointment.MarkNoShow();
        }
        catch (InvalidAppointmentStateException)
        {
            return Result.Failure<AppointmentDto>(Error.Conflict(
                "Appointment.InvalidState",
                _localizationService.GetString(LocalizationKeys.AppointmentInvalidStatusTransition)));
        }

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AppointmentDto(
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
            _localizationService.GetStatusName(appointment.Status),
            Source: appointment.Source,
            CreatedByStaffId: appointment.CreatedByStaffId,
            UtmSource: appointment.UtmSource,
            UtmMedium: appointment.UtmMedium,
            UtmCampaign: appointment.UtmCampaign,
            ExternalReference: appointment.ExternalReference,
            QueueNumber: appointment.QueueNumber));
    }
}
