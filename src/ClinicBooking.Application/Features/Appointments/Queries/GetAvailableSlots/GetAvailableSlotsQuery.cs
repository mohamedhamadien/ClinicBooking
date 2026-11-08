using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Queries.GetAvailableSlots;

public sealed record GetAvailableSlotsQuery(
    Guid TenantId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date) : IRequest<Result<IReadOnlyList<AvailableSlotDto>>>;

public sealed class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
{
    public GetAvailableSlotsQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("DoctorId is required.");
        RuleFor(x => x.ClinicId).NotEmpty().WithMessage("ClinicId is required.");
    }
}

public sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, Result<IReadOnlyList<AvailableSlotDto>>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILocalizationService _localizationService;

    public GetAvailableSlotsQueryHandler(
        IDoctorRepository doctorRepository,
        IAppointmentRepository appointmentRepository,
        IDateTimeProvider dateTimeProvider,
        ILocalizationService? localizationService = null)
    {
        _doctorRepository = doctorRepository;
        _appointmentRepository = appointmentRepository;
        _dateTimeProvider = dateTimeProvider;
        _localizationService = localizationService ?? new LocalizationService();
    }

    public async Task<Result<IReadOnlyList<AvailableSlotDto>>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive || doctor.TenantId != request.TenantId)
        {
            return Result.Failure<IReadOnlyList<AvailableSlotDto>>(Error.NotFound("Doctor", request.DoctorId));
        }

        var dayOfWeek = request.Date.DayOfWeek;
        var shifts = doctor.Availabilities
            .Where(a => a.IsActive && a.ClinicId == request.ClinicId && a.DayOfWeek == dayOfWeek)
            .ToList();

        if (shifts.Count == 0)
        {
            return Result.Success<IReadOnlyList<AvailableSlotDto>>([]);
        }

        // Fetch existing appointments for the day
        var dayStartUtc = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = request.Date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var existingAppointments = await _appointmentRepository.GetActiveAppointmentsForDoctorAsync(
            request.TenantId,
            request.DoctorId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);

        var currentUtc = _dateTimeProvider.UtcNow;
        var durationMinutes = doctor.DefaultSlotDurationMinutes;
        var slotDuration = TimeSpan.FromMinutes(durationMinutes);
        var availableSlots = new List<AvailableSlotDto>();
        var dayOfWeekName = _localizationService.GetDayOfWeekName(dayOfWeek);

        foreach (var shift in shifts)
        {
            var shiftStartUtc = request.Date.ToDateTime(shift.Shift.StartTime, DateTimeKind.Utc);
            var shiftEndUtc = request.Date.ToDateTime(shift.Shift.EndTime, DateTimeKind.Utc);

            var candidateSlotStart = shiftStartUtc;

            while (candidateSlotStart + slotDuration <= shiftEndUtc)
            {
                var candidateSlotEnd = candidateSlotStart + slotDuration;
                var slotRange = DateTimeRange.Create(candidateSlotStart, candidateSlotEnd);

                // Slot must be in the future
                var isFuture = candidateSlotStart > currentUtc;

                // Slot must not intersect with blackout exceptions
                var isBlocked = doctor.IsBlockedAt(slotRange);

                // Slot must not overlap with any active booked appointment
                var isBooked = existingAppointments.Any(a => a.ScheduledRange.OverlapsWith(slotRange));

                if (isFuture && !isBlocked && !isBooked)
                {
                    availableSlots.Add(new AvailableSlotDto(
                        doctor.Id,
                        request.ClinicId,
                        candidateSlotStart,
                        candidateSlotEnd,
                        durationMinutes,
                        dayOfWeekName));
                }

                candidateSlotStart = candidateSlotEnd;
            }
        }

        return Result.Success<IReadOnlyList<AvailableSlotDto>>(availableSlots);
    }
}
