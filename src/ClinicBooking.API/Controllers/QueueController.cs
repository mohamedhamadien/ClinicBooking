using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Queue.Commands.CheckInAppointment;
using ClinicBooking.Application.Features.Queue.Commands.StartDoctorConsultation;
using ClinicBooking.Application.Features.Queue.DTOs;
using ClinicBooking.Application.Features.Queue.Queries.GetClinicQueue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Doctor,Admin,ClinicAdmin,Staff,Receptionist,Nurse")]
public sealed class QueueController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public QueueController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Check in arriving patient at clinic reception and assign a queue token (Admin, ClinicAdmin, Doctor).
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(QueueItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QueueItemDto>> CheckIn(
        [FromBody] CheckInAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CheckInAppointmentCommand(request.AppointmentId);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Call patient into the examination room (starts consultation) (Doctor, Admin, ClinicAdmin).
    /// </summary>
    [HttpPost("{appointmentId:guid}/call")]
    [ProducesResponseType(typeof(QueueItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QueueItemDto>> StartConsultation(
        [FromRoute] Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var command = new StartDoctorConsultationCommand(appointmentId);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve current waiting room queue and token status for a clinic location (Doctor, Admin, ClinicAdmin).
    /// </summary>
    [HttpGet("clinic/{clinicId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<QueueItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QueueItemDto>>> GetQueue(
        [FromRoute] Guid clinicId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetClinicQueueQuery(tenantId, clinicId, date);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record CheckInAppointmentRequest(Guid AppointmentId);
