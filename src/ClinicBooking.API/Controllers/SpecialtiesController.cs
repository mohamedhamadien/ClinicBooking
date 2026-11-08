using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Specialties.Commands.CreateSpecialty;
using ClinicBooking.Application.Features.Specialties.DTOs;
using ClinicBooking.Application.Features.Specialties.Queries.GetSpecialties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

public sealed class SpecialtiesController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public SpecialtiesController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Retrieve all medical specialties (returns bilingual names/descriptions).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SpecialtyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SpecialtyDto>>> GetSpecialties(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSpecialtiesQuery(_tenantContext.TenantId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Add a new medical specialty with bilingual support (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SpecialtyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpecialtyDto>> CreateSpecialty(
        [FromBody] CreateSpecialtyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
