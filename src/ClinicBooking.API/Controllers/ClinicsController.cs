using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Clinics.Commands.CreateClinic;
using ClinicBooking.Application.Features.Clinics.Commands.UpdateClinic;
using ClinicBooking.Application.Features.Clinics.DTOs;
using ClinicBooking.Application.Features.Clinics.Queries.GetClinicById;
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
    /// Retrieve single clinic by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClinicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicDto>> GetClinicById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetClinicByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a clinic branch for a tenant (Super Admin only).
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

    /// <summary>
    /// Update clinic branch details (Admin or ClinicAdmin).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ClinicAdmin")]
    [ProducesResponseType(typeof(ClinicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClinicDto>> UpdateClinic(
        [FromRoute] Guid id,
        [FromBody] UpdateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClinicCommand(
            id,
            request.Name,
            request.Address,
            request.City,
            request.PhoneNumber,
            request.IsActive,
            request.NameAr,
            request.AddressAr,
            request.CityAr,
            request.SubscriptionStartDateUtc,
            request.SubscriptionEndDateUtc,
            request.SubscriptionPlan,
            request.IsPrivateClinic,
            request.IsSingleDoctorClinic,
            request.EnableProceduresDuringShifts,
            request.EnableGeneralClinicFeatures,
            request.ConsultationFee,
            request.FollowUpFee,
            request.ProcedureFee);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record UpdateClinicRequest(
    string Name,
    string Address,
    string City,
    string PhoneNumber,
    bool IsActive = true,
    string? NameAr = null,
    string? AddressAr = null,
    string? CityAr = null,
    DateTime? SubscriptionStartDateUtc = null,
    DateTime? SubscriptionEndDateUtc = null,
    string? SubscriptionPlan = null,
    bool? IsPrivateClinic = null,
    bool? IsSingleDoctorClinic = null,
    bool? EnableProceduresDuringShifts = null,
    bool? EnableGeneralClinicFeatures = null,
    decimal? ConsultationFee = null,
    decimal? FollowUpFee = null,
    decimal? ProcedureFee = null);
