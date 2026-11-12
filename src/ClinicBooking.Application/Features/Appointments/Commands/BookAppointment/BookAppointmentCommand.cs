using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;

public sealed record BookAppointmentCommand(
    Guid TenantId,
    Guid DoctorId,
    Guid PatientId,
    Guid ClinicId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? Notes = null,
    AppointmentSource Source = AppointmentSource.Website,
    Guid? CreatedByStaffId = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? ExternalReference = null) : IRequest<Result<AppointmentDto>>;

public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("DoctorId is required.");
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("PatientId is required.");
        RuleFor(x => x.ClinicId).NotEmpty().WithMessage("ClinicId is required.");

        RuleFor(x => x.StartTimeUtc)
            .NotEmpty().WithMessage("StartTimeUtc is required.")
            .Must((cmd, start) => start < cmd.EndTimeUtc)
            .WithMessage("StartTimeUtc must be earlier than EndTimeUtc.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public sealed class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public BookAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IPatientRepository patientRepository,
        ITenantRepository tenantRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILocalizationService? localizationService = null)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _tenantRepository = tenantRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _localizationService = localizationService ?? new LocalizationService();
    }

    public async Task<Result<AppointmentDto>> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        var currentUtc = _dateTimeProvider.UtcNow;
        if (request.StartTimeUtc <= currentUtc)
        {
            return Result.Failure<AppointmentDto>(Error.Validation("Appointment.PastTime",
                _localizationService.GetString(LocalizationKeys.AppointmentPastAppointment)));
        }

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Tenant", request.TenantId));
        }

        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive || doctor.TenantId != request.TenantId)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Doctor", request.DoctorId));
        }

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken)
            ?? await _patientRepository.GetByUserIdAsync(request.PatientId, cancellationToken);
        if (patient is null || patient.TenantId != request.TenantId)
        {
            return Result.Failure<AppointmentDto>(Error.NotFound("Patient", request.PatientId));
        }

        var requestedRange = DateTimeRange.Create(request.StartTimeUtc, request.EndTimeUtc);

        // 1. Validate doctor availability exceptions (blackout dates / vacations)
        if (doctor.IsBlockedAt(requestedRange))
        {
            return Result.Failure<AppointmentDto>(Error.Conflict("Doctor.BlackoutException",
                _localizationService.GetString(LocalizationKeys.AppointmentDoctorUnavailable)));
        }

        // 2. Validate doctor working hours for day of week
        var dayOfWeek = request.StartTimeUtc.DayOfWeek;
        var startLocalTime = TimeOnly.FromDateTime(request.StartTimeUtc);
        var endLocalTime = TimeOnly.FromDateTime(request.EndTimeUtc);

        var hasWorkingShift = doctor.Availabilities
            .Where(a => a.IsActive && a.ClinicId == request.ClinicId && a.DayOfWeek == dayOfWeek)
            .Any(a => a.Shift.StartTime <= startLocalTime && a.Shift.EndTime >= endLocalTime);

        if (!hasWorkingShift)
        {
            return Result.Failure<AppointmentDto>(Error.Validation("Doctor.OutsideWorkingHours",
                $"Requested time falls outside the doctor's scheduled working hours for {dayOfWeek} at the specified clinic."));
        }

        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
        {
            // 3. Check for doctor active appointment collision (under serializable transaction / lock)
            var doctorHasConflict = await _appointmentRepository.HasConflictingAppointmentForDoctorAsync(
                request.TenantId,
                request.DoctorId,
                request.StartTimeUtc,
                request.EndTimeUtc,
                excludeAppointmentId: null,
                ct);

            if (doctorHasConflict)
            {
                return Result.Failure<AppointmentDto>(Error.Conflict("Appointment.DoctorSlotUnavailable",
                    _localizationService.GetString(LocalizationKeys.AppointmentSlotAlreadyBooked)));
            }

            // 4. Check for patient active appointment collision
            var patientHasConflict = await _appointmentRepository.HasConflictingAppointmentForPatientAsync(
                request.TenantId,
                patient.Id,
                request.StartTimeUtc,
                request.EndTimeUtc,
                excludeAppointmentId: null,
                ct);

            if (patientHasConflict)
            {
                return Result.Failure<AppointmentDto>(Error.Conflict("Appointment.PatientDoubleBooking",
                    _localizationService.GetString(LocalizationKeys.AppointmentPatientOverlapping)));
            }

            // Determine if approval is needed (tenant policy OR doctor preference)
            var requiresApproval = tenant.RequiresDoctorApproval || doctor.RequiresApproval;

            var appointment = Appointment.Create(
                request.TenantId,
                request.DoctorId,
                patient.Id,
                request.ClinicId,
                requestedRange,
                currentUtc,
                requiresApproval,
                request.Notes);

            appointment.SetBookingMetadata(
                request.Source,
                request.CreatedByStaffId,
                request.UtmSource,
                request.UtmMedium,
                request.UtmCampaign,
                request.ExternalReference);

            await _appointmentRepository.AddAsync(appointment, ct);
            await _unitOfWork.SaveChangesAsync(ct);

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
                _localizationService.GetStatusName(appointment.Status),
                Source: appointment.Source,
                CreatedByStaffId: appointment.CreatedByStaffId,
                UtmSource: appointment.UtmSource,
                UtmMedium: appointment.UtmMedium,
                UtmCampaign: appointment.UtmCampaign,
                ExternalReference: appointment.ExternalReference,
                QueueNumber: appointment.QueueNumber);

            return Result.Success(dto);
        }, cancellationToken);
    }
}
