using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.ConfirmAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.MarkNoShow;
using ClinicBooking.Application.Features.Appointments.Commands.RescheduleAppointment;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Application.Features.Appointments.Queries.GetAppointmentById;
using ClinicBooking.Application.Features.Appointments.Queries.GetAvailableSlots;
using ClinicBooking.Application.Features.Appointments.Queries.GetMyAppointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize]
public sealed class AppointmentsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public AppointmentsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Query dynamically computed available appointment slots for a doctor on a specific date.
    /// </summary>
    [HttpGet("available-slots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotDto>>> GetAvailableSlots(
        [FromQuery] Guid doctorId,
        [FromQuery] Guid clinicId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var effectiveTenantId = tenantId.HasValue && tenantId.Value != Guid.Empty
            ? tenantId.Value
            : (_tenantContext.TenantId ?? Guid.Empty);

        var query = new GetAvailableSlotsQuery(effectiveTenantId, doctorId, clinicId, date);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Book an appointment slot. Protected by serializable transaction and database filtered unique indexes against concurrent double-booking.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> BookAppointment(
        [FromBody] BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId.HasValue && command.TenantId == Guid.Empty)
        {
            command = command with { TenantId = _tenantContext.TenantId.Value };
        }

        if (CurrentUserRole is "Receptionist" or "Staff" or "Admin" or "ClinicAdmin" && command.CreatedByStaffId is null)
        {
            command = command with { CreatedByStaffId = CurrentUserId };
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Confirm a scheduled or pending appointment (Receptionist, Doctor, Admin, ClinicAdmin).
    /// </summary>
    [HttpPut("{id:guid}/confirm")]
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "Doctor,Admin,ClinicAdmin,Receptionist,Staff")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Confirm(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ConfirmAppointmentCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Mark an appointment as No-Show (Receptionist, Doctor, Admin, ClinicAdmin).
    /// </summary>
    [HttpPut("{id:guid}/no-show")]
    [HttpPost("{id:guid}/no-show")]
    [Authorize(Roles = "Doctor,Admin,ClinicAdmin,Receptionist,Staff")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> MarkNoShow(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new MarkNoShowCommand(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get details of an appointment by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve upcoming and past appointments for the authenticated patient.
    /// </summary>
    [HttpGet("my-appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMyAppointments(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetMyAppointmentsQuery(tenantId, patientId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cancel an appointment in accordance with cancellation policy rules.
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var role = CurrentUserRole ?? "Patient";
        var command = new CancelAppointmentCommand(id, request.Reason, role);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reschedule an appointment to a new time window.
    /// </summary>
    [HttpPut("{id:guid}/reschedule")]
    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
}

public sealed record CancelAppointmentRequest(string Reason);

public sealed record RescheduleAppointmentRequest(DateTime NewStartTimeUtc, DateTime NewEndTimeUtc);
