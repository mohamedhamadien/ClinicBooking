using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Admin.Queries.GetAllAppointments;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.ConfirmAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.RescheduleAppointment;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Application.Features.Queue.Commands.CheckInAppointment;
using ClinicBooking.Application.Features.Queue.DTOs;
using ClinicBooking.Application.Features.Queue.Queries.GetClinicQueue;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Route("api/v1/reception")]
[Authorize(Roles = "Receptionist,Admin,ClinicAdmin,Doctor,Nurse,Staff")]
public sealed class ReceptionController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public ReceptionController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    private Guid? ResolveEffectiveClinicId(Guid? requestedClinicId)
    {
        if (_currentUserService.Role is not "Admin" && _currentUserService.ClinicId.HasValue)
        {
            return _currentUserService.ClinicId;
        }

        return requestedClinicId;
    }

    /// <summary>
    /// Retrieve daily receptionist desk summary, queue counts, and booking channel breakdown.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ReceptionDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceptionDashboardDto>> GetDashboard(
        [FromQuery] Guid? clinicId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var effectiveClinicId = ResolveEffectiveClinicId(clinicId);
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromUtc = targetDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = targetDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var apptsResult = await Mediator.Send(
            new GetAllAppointmentsQuery(tenantId, fromUtc, toUtc, null, effectiveClinicId),
            cancellationToken);

        if (apptsResult.IsFailure)
        {
            return HandleResult(apptsResult);
        }

        var appts = apptsResult.Value;
        var bySource = appts
            .GroupBy(a => a.Source.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var dto = new ReceptionDashboardDto(
            Date: targetDate,
            ClinicId: effectiveClinicId,
            TotalAppointments: appts.Count,
            ScheduledCount: appts.Count(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.PendingApproval),
            ConfirmedCount: appts.Count(a => a.Status == AppointmentStatus.Confirmed),
            CheckedInCount: appts.Count(a => a.Status == AppointmentStatus.CheckedIn),
            InConsultationCount: appts.Count(a => a.Status == AppointmentStatus.InConsultation),
            CompletedCount: appts.Count(a => a.Status == AppointmentStatus.Completed),
            CancelledCount: appts.Count(a => a.Status is AppointmentStatus.CancelledByPatient or AppointmentStatus.CancelledByDoctor or AppointmentStatus.CancelledByAdmin or AppointmentStatus.Rejected),
            NoShowCount: appts.Count(a => a.Status == AppointmentStatus.NoShow),
            BookingsBySource: bySource,
            Appointments: appts);

        return Ok(dto);
    }

    /// <summary>
    /// Retrieve unified receptionist calendar appointments with doctor, clinic, status, and source filters.
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetCalendar(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] AppointmentSource? source,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var effectiveClinicId = ResolveEffectiveClinicId(clinicId);

        var query = new GetAllAppointmentsQuery(tenantId, fromUtc, toUtc, doctorId, effectiveClinicId, status, source);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Book an appointment from the reception desk (Phone, Walk-In, WhatsApp, Facebook, Receptionist).
    /// Uses the unified domain booking logic and serializable concurrency protection.
    /// </summary>
    [HttpPost("appointments")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(
        [FromBody] ReceptionBookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? request.TenantId;
        var effectiveClinicId = ResolveEffectiveClinicId(request.ClinicId) ?? request.ClinicId;
        var source = request.Source ?? AppointmentSource.Receptionist;

        var command = new BookAppointmentCommand(
            TenantId: tenantId,
            DoctorId: request.DoctorId,
            PatientId: request.PatientId,
            ClinicId: effectiveClinicId,
            StartTimeUtc: request.StartTimeUtc,
            EndTimeUtc: request.EndTimeUtc,
            Notes: request.Notes,
            Source: source,
            CreatedByStaffId: CurrentUserId,
            UtmSource: request.UtmSource,
            UtmMedium: request.UtmMedium,
            UtmCampaign: request.UtmCampaign,
            ExternalReference: request.ExternalReference);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Register a walk-in appointment and optionally check the patient directly into the live clinic queue.
    /// </summary>
    [HttpPost("walk-ins")]
    [ProducesResponseType(typeof(WalkInRegistrationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WalkInRegistrationResultDto>> RegisterWalkIn(
        [FromBody] RegisterWalkInRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? request.TenantId;
        var effectiveClinicId = ResolveEffectiveClinicId(request.ClinicId) ?? request.ClinicId;

        var bookCommand = new BookAppointmentCommand(
            TenantId: tenantId,
            DoctorId: request.DoctorId,
            PatientId: request.PatientId,
            ClinicId: effectiveClinicId,
            StartTimeUtc: request.StartTimeUtc,
            EndTimeUtc: request.EndTimeUtc,
            Notes: request.Notes ?? "Walk-in patient registered at reception desk",
            Source: AppointmentSource.WalkIn,
            CreatedByStaffId: CurrentUserId);

        var bookResult = await Mediator.Send(bookCommand, cancellationToken);
        if (bookResult.IsFailure)
        {
            return HandleResult(bookResult);
        }

        QueueItemDto? queueItem = null;
        if (request.ImmediateCheckIn)
        {
            var checkInResult = await Mediator.Send(new CheckInAppointmentCommand(bookResult.Value.Id), cancellationToken);
            if (checkInResult.IsSuccess)
            {
                queueItem = checkInResult.Value;
            }
        }

        return Ok(new WalkInRegistrationResultDto(bookResult.Value, queueItem));
    }

    /// <summary>
    /// Check in an arriving patient at the reception desk and assign a queue token.
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(QueueItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QueueItemDto>> CheckIn(
        [FromBody] CheckInAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CheckInAppointmentCommand(request.AppointmentId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reschedule an appointment from the reception desk.
    /// </summary>
    [HttpPut("appointments/{id:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        [FromRoute] Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RescheduleAppointmentCommand(id, request.NewStartTimeUtc, request.NewEndTimeUtc);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Confirm an appointment from the reception desk.
    /// </summary>
    [HttpPost("appointments/{id:guid}/confirm")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentDto>> Confirm(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ConfirmAppointmentCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cancel an appointment from the reception desk.
    /// </summary>
    [HttpPost("appointments/{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentDto>> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var role = CurrentUserRole ?? "Receptionist";
        var result = await Mediator.Send(new CancelAppointmentCommand(id, request.Reason, role), cancellationToken);
        return HandleResult(result);
    }
}

public sealed record ReceptionDashboardDto(
    DateOnly Date,
    Guid? ClinicId,
    int TotalAppointments,
    int ScheduledCount,
    int ConfirmedCount,
    int CheckedInCount,
    int InConsultationCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    IReadOnlyDictionary<string, int> BookingsBySource,
    IReadOnlyList<AppointmentDto> Appointments);

public sealed record ReceptionBookAppointmentRequest(
    Guid TenantId,
    Guid DoctorId,
    Guid PatientId,
    Guid ClinicId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? Notes = null,
    AppointmentSource? Source = AppointmentSource.Receptionist,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? ExternalReference = null);

public sealed record RegisterWalkInRequest(
    Guid TenantId,
    Guid DoctorId,
    Guid PatientId,
    Guid ClinicId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? Notes = null,
    bool ImmediateCheckIn = true);

public sealed record WalkInRegistrationResultDto(
    AppointmentDto Appointment,
    QueueItemDto? QueueItem);
