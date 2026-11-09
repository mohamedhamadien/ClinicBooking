using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Prescriptions.Commands.CreatePrescription;
using ClinicBooking.Application.Features.Prescriptions.DTOs;
using ClinicBooking.Application.Features.Prescriptions.Queries.GetPatientPrescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize]
public sealed class PrescriptionsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public PrescriptionsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Issue an official electronic prescription (Rx) with medications (Doctor, Admin, ClinicAdmin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin,ClinicAdmin")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PrescriptionDto>> CreatePrescription(
        [FromBody] CreatePrescriptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve all electronic prescriptions for a patient.
    /// </summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PrescriptionDto>>> GetPatientPrescriptions(
        [FromRoute] Guid patientId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetPatientPrescriptionsQuery(tenantId, patientId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
