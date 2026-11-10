using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Insurance.Commands.CreateInsuranceProvider;
using ClinicBooking.Application.Features.Insurance.DTOs;
using ClinicBooking.Application.Features.Insurance.Queries.GetInsuranceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize]
public sealed class InsuranceController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public InsuranceController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyList<InsuranceProviderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InsuranceProviderDto>>> GetProviders(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetInsuranceProvidersQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("providers")]
    [Authorize(Roles = "Admin,ClinicAdmin")]
    [ProducesResponseType(typeof(InsuranceProviderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InsuranceProviderDto>> CreateProvider(
        [FromBody] CreateInsuranceProviderCommand command,
        CancellationToken cancellationToken)
    {
        var finalCommand = command with { TenantId = _tenantContext.TenantId };
        var result = await Mediator.Send(finalCommand, cancellationToken);
        return HandleResult(result);
    }
}
