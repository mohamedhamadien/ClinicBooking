using ClinicBooking.Application.Features.Tenants.Commands.CreateTenant;
using ClinicBooking.Application.Features.Tenants.DTOs;
using ClinicBooking.Application.Features.Tenants.Queries.GetTenantById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

public sealed class TenantsController : ApiControllerBase
{
    /// <summary>
    /// Retrieve tenant details by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantDto>> GetTenantById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Provision a new tenant organization (Host Administrator only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TenantDto>> CreateTenant(
        [FromBody] CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
