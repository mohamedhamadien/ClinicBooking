using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Clinics.Commands.CreateClinic;
using ClinicBooking.Application.Features.Clinics.DTOs;
using ClinicBooking.Application.Features.Clinics.Queries.GetClinics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

public sealed class ClinicsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public ClinicsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Retrieve all clinics for the active tenant.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ClinicDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClinicDto>>> GetClinics(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var result = await Mediator.Send(new GetClinicsQuery(tenantId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a clinic branch for a tenant (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClinicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClinicDto>> CreateClinic(
        [FromBody] CreateClinicCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
