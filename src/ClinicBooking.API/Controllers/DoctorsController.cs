using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Doctors.Commands.AddAvailabilityException;
using ClinicBooking.Application.Features.Doctors.Commands.CreateDoctor;
using ClinicBooking.Application.Features.Doctors.Commands.SetDoctorAvailability;
using ClinicBooking.Application.Features.Doctors.DTOs;
using ClinicBooking.Application.Features.Doctors.Queries.GetDoctorById;
using ClinicBooking.Application.Features.Doctors.Queries.GetDoctors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

public sealed class DoctorsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public DoctorsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Search and filter doctors by specialty, clinic, or name/bio.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetDoctors(
        [FromQuery] Guid? specialtyId,
        [FromQuery] Guid? clinicId,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetDoctorsQuery(tenantId, specialtyId, clinicId, searchTerm);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve detailed doctor profile with schedule.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> GetDoctorById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDoctorByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new doctor profile (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DoctorDto>> CreateDoctor(
        [FromBody] CreateDoctorCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Configure working shift for a doctor (Doctor or Admin).
    /// </summary>
    [HttpPost("{id:guid}/availability")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetAvailability(
        [FromRoute] Guid id,
        [FromBody] SetDoctorAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetDoctorAvailabilityCommand(
            id,
            request.ClinicId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Block out doctor availability dates/times (Doctor or Admin).
    /// </summary>
    [HttpPost("{id:guid}/availability-exceptions")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddAvailabilityException(
        [FromRoute] Guid id,
        [FromBody] AddAvailabilityExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddAvailabilityExceptionCommand(
            id,
            request.StartUtc,
            request.EndUtc,
            request.Reason);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record SetDoctorAvailabilityRequest(
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record AddAvailabilityExceptionRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason);
